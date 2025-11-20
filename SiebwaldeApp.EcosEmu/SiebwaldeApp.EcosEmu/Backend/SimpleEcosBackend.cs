using System.Linq;
using System.Text.RegularExpressions;
namespace SiebwaldeApp.EcosEmu
{
    public class SimpleEcosBackend : IEcosBackend, IHardwareFeedbackSink
    {
        private readonly Dictionary<int, LocoState> _locos = new();
        private readonly Dictionary<int, SwitchState> _switchStates = new();
        private readonly object _switchStateLock = new();
        private readonly IHardwareBackend _hardware;
        private readonly ILocoRepository _locoRepository;
        private readonly IBlockPositionProvider _blockPositionProvider;
        // Feedback modules (S88 / occupancy). Key = module id (e.g. 100).
        private readonly Dictionary<int, FeedbackModule> _feedbackModules = new();
        private readonly Dictionary<int, int> _sensorModuleStates = new();
        private readonly object _sensorStateLock = new();

        /// <summary>
        /// Raised whenever the backend wants to send an unsolicited ECoS-style event
        /// to the active TCP client (e.g. Koploper).
        /// Host layer must subscribe to this.
        /// </summary>
        public event Func<string, Task>? EventGenerated;

        /// <summary>
        /// Represents the current <see cref="TextWriter"/> instance being used for output operations.
        /// </summary>
        /// <remarks>This field may be <see langword="null"/> if no writer is currently
        /// assigned.</remarks>
        private TextWriter? _currentWriter;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleEcosBackend"/> class,  providing integration with
        /// hardware, locomotive data, and block position tracking.
        /// </summary>
        /// <remarks>This constructor sets up the backend by subscribing to block entry events from the 
        /// <paramref name="blockPositionProvider"/>. These events are used to update the internal state  and
        /// synchronize locomotive positions.</remarks>
        /// <param name="hardware">The hardware backend used to communicate with the physical ecosystem.  This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="locoRepository">The repository that provides access to locomotive data and state.  This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="blockPositionProvider">The provider responsible for tracking block positions and reporting block entry events.  This parameter
        /// cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="hardware"/>, <paramref name="locoRepository"/>, or  <paramref
        /// name="blockPositionProvider"/> is <see langword="null"/>.</exception>
        public SimpleEcosBackend(
            IHardwareBackend hardware,
            ILocoRepository locoRepository,
            IBlockPositionProvider blockPositionProvider)
        {
            _hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
            _locoRepository = locoRepository ?? throw new ArgumentNullException(nameof(locoRepository));
            _blockPositionProvider = blockPositionProvider ?? throw new ArgumentNullException(nameof(blockPositionProvider));

            // Update internal state and persistent locos when Koploper reports a new block
            _blockPositionProvider.BlockEntered += OnBlockEntered;

            // Define a single feedback module (group 1 in Koploper).
            // In Koploper this corresponds to feedback group 1.x → module id 100.
            _feedbackModules[100] = new FeedbackModule
            {
                Id = 100,
                Ports = 16,
                StateMask = 0
            };
        }

        private void OnBlockEntered(int locoAddress, int blockNumber)
        {
            // Update runtime loco state if present
            var state = _locos.Values.FirstOrDefault(l => l.Address == locoAddress);
            if (state != null)
            {
                state.Block = blockNumber;
            }

            // Update persistent loco info
            var info = _locoRepository.GetByAddress(locoAddress);
            if (info != null)
            {
                info.Block = blockNumber;
                _locoRepository.AddOrUpdate(info);

                // Fire-and-forget: do not block Koploper thread on disk I/O
                _ = _locoRepository.SaveAsync();
            }

            Console.WriteLine($"[LOCO] Address {locoAddress} is now in block {blockNumber}.");
        }
        
        /// <summary>
        /// Handles the specified command by delegating to the appropriate handler based on the command name.
        /// </summary>
        /// <remarks>The method supports a variety of command types, such as "set", "get", "queryObjects",
        /// "request",  "release", "create", and "delete". For unrecognized commands, a response indicating an unknown 
        /// command is written to the <paramref name="writer"/>.</remarks>
        /// <param name="cmd">The command to process, including its name and associated parameters.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the response for the command.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task HandleAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // Remember the current client writer so we can send unsolicited events later
            _currentWriter = writer;

            switch (cmd.Name)
            {
                case "set":
                    await HandleSetAsync(cmd, writer, ct);
                    break;
                case "get":
                    await HandleGetAsync(cmd, writer, ct);
                    break;
                case "queryObjects":
                    await HandleQueryObjectsAsync(cmd, writer, ct);
                    break;
                case "request":
                    await HandleRequestAsync(cmd, writer, ct);
                    break;
                case "release":
                    await HandleReleaseAsync(cmd, writer, ct);
                    break;
                case "create":
                    await HandleCreateAsync(cmd, writer, ct);
                    break;
                case "delete":
                    await HandleDeleteAsync(cmd, writer, ct);
                    break;
                default:
                    await WriteReplyAsync(writer, cmd.RawLine, 1, "UNKNOWN_COMMAND", null);
                    break;
            }
        }

        /// <summary>
        /// Handles a request command asynchronously, processes locomotive state, and writes a response.
        /// </summary>
        /// <remarks>This method processes a command to retrieve or update the state of a locomotive. If
        /// the locomotive state is not already loaded, it attempts to retrieve the state from the repository and
        /// initializes it. The response includes the locomotive's speed and direction, but function states are excluded
        /// to avoid compatibility issues with certain clients.</remarks>
        /// <param name="cmd">The command to process, which includes the object ID and raw command line.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the response.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task HandleRequestAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // REQUEST <id>,<view/control/force/...>
            // Example from Koploper:
            //   request(100,view)
            //   request(1000,view)
            //
            // We must answer with the current state of the object.
            // For locomotives: speed / dir
            // For feedback modules: state[...]

            if (cmd.ObjectId == null)
            {
                await WriteReplyAsync(writer, cmd.RawLine, 2, "MISSING_ID", null);
                return;
            }

            int id = cmd.ObjectId.Value;

            //
            // 1) FEEDBACK MODULE (S88 / occupancy sensors)
            //    (KOPLOPER WILL CALL: request(100,view))
            //
            if (_feedbackModules.TryGetValue(id, out var fbModule))
            {
                // Example reply Koploper expects:
                // <REPLY request(100,view)>
                // 100 state[0]
                // <END 0 (OK)>

                var lines = new List<string>
        {
            $"{fbModule.Id} state[{fbModule.StateMask}]"
        };

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                return;
            }

            //
            // 2) LOCOMOTIVE REQUEST
            //
            if (_locos.TryGetValue(id, out var loco))
            {
                var lines = new List<string>
        {
            $"{id} speed[{loco.Speed}]",
            $"{id} dir[{loco.Direction}]"
        };

                // We do NOT return func[...] here → Koploper crashes on startup otherwise.

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                return;
            }

            //
            // 3) OBJECT EXISTS IN REPOSITORY BUT NOT IN _locos YET (lazy init)
            //
            var info = _locoRepository.GetByEcosId(id);
            if (info != null)
            {
                loco = new LocoState
                {
                    EcosId = info.EcosId,
                    Address = info.Address,
                    Block = info.Block,
                    Speed = 0,
                    Direction = 1
                };

                // Load stored functions (but do NOT send them back!)
                loco.LoadFunctionsFromInfo(info);

                _locos[id] = loco;

                var lines = new List<string>
        {
            $"{id} speed[{loco.Speed}]",
            $"{id} dir[{loco.Direction}]"
        };

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                return;
            }

            //
            // 4) UNKNOWN OBJECT → return OK with empty body
            //
            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
        }
        //private async Task HandleRequestAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        //{
        //    // Example: request(1000,control, force) or request(1000,view)

        //    if (cmd.ObjectId == null)
        //    {
        //        await WriteReplyAsync(writer, cmd.RawLine, 2, "MISSING_ID", null);
        //        return;
        //    }

        //    int id = (int)cmd.ObjectId;

        //    if (!_locos.TryGetValue(id, out var loco))
        //    {
        //        // Try to create state from repository if not present yet
        //        var info = _locoRepository.GetByEcosId(id);
        //        if (info != null)
        //        {
        //            loco = new LocoState
        //            {
        //                EcosId = info.EcosId,
        //                Address = info.Address,
        //                Block = info.Block,
        //                Speed = 0,
        //                Direction = 1
        //            };

        //            // Load function states from JSON if available
        //            loco.LoadFunctionsFromInfo(info);

        //            _locos[info.EcosId] = loco;
        //        }
        //    }

        //    var lines = new List<string>();

        //    if (loco != null)
        //    {
        //        // Always report speed and direction
        //        lines.Add($"{id} speed[{loco.Speed}]");
        //        lines.Add($"{id} dir[{loco.Direction}]");

        //        // IMPORTANT:
        //        // Do not report all function states here, because Koploper appears
        //        // to crash when func[...] lines are returned as part of a request(...)
        //        // reply at startup.
        //        //
        //        // The function states are still kept in memory and JSON and will
        //        // be reflected via func[...] events when Koploper changes them.
        //        //
        //        //for (int i = 0; i < loco.Functions.Length; i++)
        //        //{
        //        //    int val = loco.Functions[i] ? 1 : 0;
        //        //    lines.Add($"{id} func[{i}][{val}]");
        //        //}
        //    }

        //    await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
        //}

        /// <summary>
        /// Handles the release command by processing the specified <see cref="EcosCommand"/> and writing an
        /// acknowledgment response to the provided <see cref="TextWriter"/>.
        /// </summary>
        /// <remarks>This method ensures that the caller receives an "OK" acknowledgment for the provided
        /// command. Additional events may be sent later as required.</remarks>
        /// <param name="cmd">The command to process, containing the raw input line and associated data.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to send the response back to the caller.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task HandleReleaseAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // Voor nu heel simpel: accepteer alles.
            // request(1,view)
            // request(10,view)
            // Koploper verwacht alleen "OK" en evt. events later.

            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
        }

        /// <summary>
        /// Handles the "set" command by processing the specified options and updating the state of the system
        /// accordingly.
        /// </summary>
        /// <remarks>This method processes commands for both the central system (e.g., power on/off) and
        /// locomotive-specific operations. For locomotive commands, it updates the state of the locomotive (e.g.,
        /// speed, direction, address, functions) and persists changes to the repository. Events generated during
        /// processing are sent back to the client.</remarks>
        /// <param name="cmd">The command containing the object ID and options to process. The <see cref="EcosCommand.ObjectId"/> property
        /// must not be null.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to send replies and events back to the client.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task HandleSetAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            if (cmd.ObjectId == null)
            {
                await WriteReplyAsync(writer, cmd.RawLine, 2, "MISSING_ID", null);
                return;
            }

            int id = (int)cmd.ObjectId;

            // Base object: power on/off of the central
            if (id == 1)
            {
                foreach (var opt in cmd.Options)
                {
                    if (opt.StartsWith("go", StringComparison.OrdinalIgnoreCase))
                        _hardware.SetPower(true);
                    else if (opt.StartsWith("stop", StringComparison.OrdinalIgnoreCase))
                        _hardware.SetPower(false);
                }

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
                return;
            }

            // Locos (simplified: all other IDs are handled as locos)
            if (!_locos.TryGetValue(id, out var loco))
            {
                var info = _locoRepository.GetByEcosId(id);

                loco = new LocoState
                {
                    EcosId = id,
                    Address = info?.Address ?? id,
                    Block = info?.Block
                };

                // Load function states from JSON if available
                if (info != null)
                    loco.LoadFunctionsFromInfo(info);

                _locos[id] = loco;
            }

            var events = new List<string>();

            foreach (var opt in cmd.Options)
            {
                // 1) turnout / switch handling
                if (opt.StartsWith("switch", StringComparison.OrdinalIgnoreCase) ||
                    opt.StartsWith("state", StringComparison.OrdinalIgnoreCase))
                {
                    HandleSwitchCommand(id, opt, events);
                    continue;
                }
                //// 1) SWITCH/WISSEL-commando's
                //if (opt.StartsWith("switch", StringComparison.OrdinalIgnoreCase) ||
                //    opt.StartsWith("state", StringComparison.OrdinalIgnoreCase))
                //{
                //    // Laat de helper het zware werk doen
                //    HandleSwitchCommand(id, opt);

                //    // Voor nu geen events terugsturen; Koploper lijkt die voor wissels niet nodig te hebben.
                //    // Als je exact ECoS-gedrag wilt, kun je hier evt. nog iets in 'events' zetten.
                //    continue;
                //}

                // 2) LOCO-commando's
                if (opt.StartsWith("speed", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, value) = ParseBracketInt(opt);
                    if (ok)
                    {
                        loco.Speed = value;
                        _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);

                        // Event: reflect new speed
                        events.Add($"{id} speed[{loco.Speed}]");
                    }
                }
                else if (opt.StartsWith("dir", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, value) = ParseBracketInt(opt);
                    if (ok)
                    {
                        loco.Direction = value;
                        _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);

                        // Event: reflect new direction
                        events.Add($"{id} dir[{loco.Direction}]");
                    }
                }
                else if (opt.StartsWith("addr", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, value) = ParseBracketInt(opt);
                    if (ok)
                    {
                        loco.Address = value;

                        var info = _locoRepository.GetByEcosId(id) ?? new LocoInfo { EcosId = id };
                        info.Address = value;
                        info.Block = loco.Block;
                        _locoRepository.AddOrUpdate(info);
                        await _locoRepository.SaveAsync(ct);
                    }
                }
                else if (opt.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, text) = ParseBracketString(opt);
                    if (ok)
                    {
                        var info = _locoRepository.GetByEcosId(id) ?? new LocoInfo { EcosId = id };
                        info.Name = text;
                        info.Address = info.Address == 0 ? loco.Address : info.Address;
                        info.Block = loco.Block;
                        _locoRepository.AddOrUpdate(info);
                        await _locoRepository.SaveAsync(ct);
                    }
                }
                else if (opt.StartsWith("func", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, index, value) = ParseFunctionOption(opt);
                    if (ok && index >= 0 && index < loco.Functions.Length)
                    {
                        bool on = value != 0;
                        loco.Functions[index] = on;

                        // Update JSON
                        var info = _locoRepository.GetByEcosId(id);
                        if (info == null)
                        {
                            info = new LocoInfo { EcosId = id, Address = loco.Address };
                        }

                        // Store function state back into persistent info
                        loco.SaveFunctionsToInfo(info);

                        _locoRepository.AddOrUpdate(info);
                        await _locoRepository.SaveAsync(ct);

                        // Event back to Koploper
                        events.Add($"{id} func[{index}][{(on ? 1 : 0)}]");
                    }
                }
                else if (opt.StartsWith("speedstep", StringComparison.OrdinalIgnoreCase))
                {
                    // Koploper sometimes sends speedstep[...]; for now we just ignore or log it.
                    // var (ok, value) = ParseBracketInt(opt);
                    // if (ok) { /* store if you want */ }
                }
            }

            // Reply to Koploper, including any event lines we collected

            // Reply state to koploper
            //var lines = events.Count > 0 ? events : null;
            //await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);

            // Without state to koploper
            // Reply to Koploper – for switch events we don't echo them in the reply body
            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);

            // Additionally send ECoS-style events back to the client (e.g. Koploper).
            foreach (var ev in events)
            {
                await WriteEventAsync(writer, ev);
            }
        }

        /// <summary>
        /// Handles a GET command by processing the specified object ID and options, and writes the response to the
        /// provided writer.
        /// </summary>
        /// <remarks>This method processes the GET command by evaluating the object ID and any associated
        /// options.  - If the object ID is missing, an error response is written. - For object ID 1 with the "info"
        /// option, a special informational response is returned. - For other object IDs, the method retrieves
        /// locomotive data (if available) and processes options such as "speed" and "dir" to generate the response.  If
        /// the object ID does not exist in the internal collection, default values are used in the response.</remarks>
        /// <param name="cmd">The command containing the object ID and options to process. The <see cref="EcosCommand.ObjectId"/> property
        /// must be set for the command to be valid.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the response back to the caller.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns></returns>
        private async Task HandleGetAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            if (cmd.ObjectId == null)
            {
                await WriteReplyAsync(writer, cmd.RawLine, 2, "MISSING_ID", null);
                return;
            }

            int id = cmd.ObjectId.Value;

            // Special case: base object ECoS info
            if (id == 1 && cmd.Options.Length > 0 &&
                string.Equals(cmd.Options[0], "info", StringComparison.OrdinalIgnoreCase))
            {
                var lines = new List<string>
        {
            "1 info[\"SiebwaldeEmu 0.2\"]"
        };

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                return;
            }

            // --- NEW: Feedback module support (S88 / detector modules) ---
            // ECoS spec: S88Modul is a list object with dynamic IDs (e.g. 100)
            //   get(id, state)  -> "<REPLY ...>\n100 state[mask]\n<END 0 (OK)>"
            //   get(id, ports)  -> "<REPLY ...>\n100 ports[16]\n<END 0 (OK)>"
            if (_feedbackModules.TryGetValue(id, out var fbModule))
            {
                var lines = new List<string>();

                foreach (var opt in cmd.Options)
                {
                    if (string.Equals(opt, "state", StringComparison.OrdinalIgnoreCase))
                    {
                        lines.Add($"{fbModule.Id} state[{fbModule.StateMask}]");
                    }
                    else if (string.Equals(opt, "ports", StringComparison.OrdinalIgnoreCase))
                    {
                        lines.Add($"{fbModule.Id} ports[{fbModule.Ports}]");
                    }
                }

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                return;
            }

            // --- Existing loco handling (fallback) ---
            _locos.TryGetValue(id, out var loco);

            var replyLines = new List<string>();

            foreach (var opt in cmd.Options)
            {
                if (string.Equals(opt, "speed", StringComparison.OrdinalIgnoreCase))
                    replyLines.Add($"{id} speed[{loco?.Speed ?? 0}]");
                else if (string.Equals(opt, "dir", StringComparison.OrdinalIgnoreCase))
                    replyLines.Add($"{id} dir[{loco?.Direction ?? 1}]");
            }

            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", replyLines);
        }

        //private async Task HandleGetAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        //{
        //    if (cmd.ObjectId == null)
        //    {
        //        await WriteReplyAsync(writer, cmd.RawLine, 2, "MISSING_ID", null);
        //        return;
        //    }

        //    int id = cmd.ObjectId.Value;

        //    // Special case: basisobject ECoS info
        //    if (id == 1 && cmd.Options.Length > 0 &&
        //        string.Equals(cmd.Options[0], "info", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var lines = new List<string>
        //        {
        //            "1 info[\"SiebwaldeEmu 0.2\"]"
        //        };

        //        await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
        //        return;
        //    }

        //    _locos.TryGetValue(id, out var loco);

        //    var replyLines = new List<string>();

        //    foreach (var opt in cmd.Options)
        //    {
        //        if (string.Equals(opt, "speed", StringComparison.OrdinalIgnoreCase))
        //            replyLines.Add($"{id} speed[{loco?.Speed ?? 0}]");
        //        else if (string.Equals(opt, "dir", StringComparison.OrdinalIgnoreCase))
        //            replyLines.Add($"{id} dir[{loco?.Direction ?? 1}]");
        //    }

        //    await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", replyLines);
        //}

        /// <summary>
        /// Handles the query objects command by processing requests for specific object managers and writing the
        /// appropriate response to the provided <see cref="TextWriter"/>.
        /// </summary>
        /// <remarks>This method supports querying specific object managers, such as the locomotive
        /// manager (ID = 10)  and the feedback manager (ID = 26). The response format and content depend on the object
        /// manager ID  and any options provided in the command. <list type="bullet"> <item> For the locomotive manager
        /// (ID = 10), the method returns a list of locomotives, including their  address, extended address, name, and
        /// protocol. </item> <item> For the feedback manager (ID = 26), the method supports two modes: <list
        /// type="bullet"> <item>If no options are provided, it returns a list of feedback module IDs.</item> <item>If
        /// the "ports" option is provided, it returns a list of feedback modules with their port counts.</item> </list>
        /// </item> <item> For unsupported object manager IDs or unknown options, the method responds with "OK" but does
        /// not include a body. </item> </list></remarks>
        /// <param name="cmd">The command containing the object manager ID, options, and raw input line.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the response.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the operation to complete.</param>
        /// <returns></returns>
        private async Task HandleQueryObjectsAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // LokManager (ID = 10) → list locomotives
            if (cmd.ObjectId == 10)
            {
                var locos = _locoRepository
                    .GetAll()
                    .OrderBy(l => l.EcosId)
                    .ToList();

                var lines = new List<string>();

                foreach (var loco in locos)
                {
                    int addrext = loco.AddressExt ?? 0;
                    string safeName = loco.Name?.Replace("\"", "\\\"") ?? string.Empty;
                    string protocol = loco.Protocol ?? "DCC28";

                    string line =
                        $"{loco.EcosId} " +
                        $"addr[{loco.Address}] " +
                        $"addrext[{addrext}] " +
                        $"name[\"{safeName}\"] " +
                        $"protocol[\"{protocol}\"]";

                    lines.Add(line);
                }

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                return;
            }

            // Feedback Manager (ID = 26) → list feedback modules / ports
            if (cmd.ObjectId == 26)
            {
                var lines = new List<string>();

                // No options: just list module IDs, e.g. "100"
                if (cmd.Options.Length == 0)
                {
                    // We bieden één module aan: 100 met 16 ingangen (groep 1 in Koploper)
                    lines.Add("100");

                    await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                    return;
                }

                // Example: queryObjects(26,ports)
                if (cmd.Options.Length == 1 &&
                    string.Equals(cmd.Options[0], "ports", StringComparison.OrdinalIgnoreCase))
                {
                    // Module 100 heeft 16 ingangen
                    lines.Add("100 ports[16]");

                    await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
                    return;
                }

                // Onbekende optie → wel OK maar zonder body
                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
                return;
            }

            // Default: we emuleren andere managers nog niet
            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
        }

        /// <summary>
        /// Handles the creation of a new locomotive in the system based on the provided command.
        /// </summary>
        /// <remarks>This method processes a "create" command for locomotives (class 10). It assigns a
        /// unique identifier to the new locomotive,  parses optional parameters such as name, protocol, and address,
        /// and creates both persistent and runtime state for the locomotive. If the command specifies an unsupported
        /// object class, an error response is returned.</remarks>
        /// <param name="cmd">The command containing the creation request, including options such as name, protocol, and address.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to write the response back to the caller.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns></returns>
        private async Task HandleCreateAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // "create(10,...)" means: create new locomotive in LokManager (class 10)
            if (cmd.ObjectId != 10)
            {
                // For now we only support creating locomotives (class 10).
                await WriteReplyAsync(writer, cmd.RawLine, 1, "UNKNOWN_COMMAND", null);
                return;
            }

            // Determine a new ECoS object id for this loco.
            // We keep it simple: start at 1000 and count up.
            int newId = 1000;
            var all = _locoRepository.GetAll();
            if (all.Any())
            {
                int maxId = all.Max(l => l.EcosId);
                newId = Math.Max(1000, maxId + 1);
            }

            string name = "New Loco";
            string protocol = "DCC28";
            int address = 0;
            int? addrext = null;

            // Parse options from create(10,name["TestLok 3"],protocol[DCC28],addr[3],append)
            foreach (var opt in cmd.Options)
            {
                if (opt.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, text) = ParseBracketString(opt);
                    if (ok && !string.IsNullOrWhiteSpace(text))
                    {
                        name = text;
                    }
                }
                else if (opt.StartsWith("protocol", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, text) = ParseBracketString(opt);
                    if (ok && !string.IsNullOrWhiteSpace(text))
                    {
                        protocol = text;
                    }
                }
                else if (opt.StartsWith("addr", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, value) = ParseBracketInt(opt);
                    if (ok)
                    {
                        address = value;
                    }
                }
                else if (opt.StartsWith("addrext", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, value) = ParseBracketInt(opt);
                    if (ok)
                    {
                        addrext = value;
                    }
                }
                // "append" is just a flag, we can safely ignore it.
            }

            // Create persistent loco info
            var info = new LocoInfo
            {
                EcosId = newId,
                Name = name,
                Address = address,
                AddressExt = addrext,
                Protocol = protocol,
                Block = null
            };

            _locoRepository.AddOrUpdate(info);
            await _locoRepository.SaveAsync(ct);

            // Create runtime loco state
            var state = new LocoState
            {
                EcosId = newId,
                Address = address,
                Direction = 1,
                Speed = 0,
                Block = _blockPositionProvider.TryGetBlockForLoc(address)
            };

            _locos[newId] = state;

            Console.WriteLine($"[ECOS] Created loco {newId}: name='{name}', addr={address}, protocol={protocol}.");

            // ECoS typically returns the new object id in the reply body.
            // This allows the client (Koploper) to learn the new id.
            var lines = new List<string> { newId.ToString() };

            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
        }

        /// <summary>
        /// Handles the deletion of an object or locomotive based on the specified command.
        /// </summary>
        /// <remarks>This method processes two types of delete commands: <list type="bullet"> <item> If
        /// the object ID is 10, the method expects an "id" parameter in the command options to identify the locomotive
        /// to delete. </item> <item> For other object IDs, the method directly deletes the object with the specified
        /// ID. </item> </list> The method removes the object or locomotive from both the runtime state and the
        /// persistent repository, if applicable. A response is written to the caller indicating the success or failure
        /// of the operation.</remarks>
        /// <param name="cmd">The command containing the object ID and optional parameters for the deletion operation.</param>
        /// <param name="writer">The <see cref="TextWriter"/> used to send the response back to the caller.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns></returns>
        private async Task HandleDeleteAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            int ecosId = -1;

            if (cmd.ObjectId == 10)
            {
                // delete from locomotive manager:
                //   delete(10,id[1000])
                foreach (var opt in cmd.Options)
                {
                    if (opt.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                    {
                        var (ok, value) = ParseBracketInt(opt);
                        if (ok)
                        {
                            ecosId = value;
                            break;
                        }
                    }
                }

                if (ecosId < 0)
                {
                    await WriteReplyAsync(writer, cmd.RawLine, 1, "MISSING_ID", null);
                    return;
                }
            }
            else
            {
                // direct delete of an object:
                //   delete(1000)
                ecosId = (int)cmd.ObjectId;
            }

            // Remove from runtime loco state
            if (_locos.Remove(ecosId))
            {
                Console.WriteLine($"[LOCO] Removed runtime loco state for id={ecosId}.");
            }

            // Remove from persistent repository
            var info = _locoRepository.GetByEcosId(ecosId);
            if (info != null)
            {
                _locoRepository.RemoveByEcosId(ecosId);
                await _locoRepository.SaveAsync(ct);
                Console.WriteLine($"[LOCO] Removed persistent loco id={ecosId}.");
            }

            // Reply OK to Koploper
            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
        }

        /// <summary>
        /// Handles a change in the state of a switch and sends the corresponding event to the active writer.
        /// </summary>
        /// <remarks>This method updates the internal state of the switch, creates an event representing
        /// the change,  and writes the event to the currently active writer. If no writer is active, the event is not
        /// sent,  and a message is logged to indicate this condition.</remarks>
        /// <param name="ecosId">The identifier of the ecosystem where the switch resides.</param>
        /// <param name="decoderAddress">The address of the decoder controlling the switch.</param>
        /// <param name="outputIndex">The index of the output within the decoder that corresponds to the switch.</param>
        /// <returns></returns>
        public async Task OnSwitchChangedAsync(int ecosId, int decoderAddress, int outputIndex)
        {
            // Update internal state and build the event line
            var ev = UpdateSwitchStateAndCreateEvent(ecosId, decoderAddress, outputIndex);

            Console.WriteLine($"[HW-FEEDBACK] {ev}");

            var writer = _currentWriter;
            if (writer == null)
            {
                Console.WriteLine("  !! No active writer for switch feedback; event not sent.");
                return;
            }

            await WriteEventAsync(writer, ev);
        }

        /// <summary>
        /// Handles a sensor state change event by updating the state of the corresponding feedback module and sending
        /// the updated state to the active writer.
        /// </summary>
        /// <remarks>This method maps the logical sensor ID to a predefined feedback module and updates
        /// the module's state mask to reflect the sensor's occupancy status. If the sensor ID is out of range or the
        /// feedback module is not defined, the event is ignored, and a message is logged.  The updated state mask is
        /// sent to the active writer in the format expected by the ECoS convention. If no active writer is available,
        /// the event is not sent, and a message is logged.</remarks>
        /// <param name="sensorId">The identifier of the sensor that triggered the event. Must be between 1 and the number of ports in the
        /// feedback module.</param>
        /// <param name="occupied">A value indicating whether the sensor is occupied (<see langword="true"/>) or unoccupied (<see
        /// langword="false"/>).</param>
        /// <returns></returns>
        public async Task OnSensorChangedAsync(int sensorId, bool occupied)
        {
            const int moduleId = 100;   // Koploper group 1

            if (!_feedbackModules.TryGetValue(moduleId, out var module))
            {
                Console.WriteLine($"[HW-FEEDBACK] No feedback module {moduleId}");
                return;
            }

            if (sensorId < 1 || sensorId > module.Ports)
            {
                Console.WriteLine($"[HW-FEEDBACK] Out of range sensor {sensorId}");
                return;
            }

            int bit = sensorId - 1;     // 1→0, 2→1, etc.

            if (occupied)
                module.StateMask |= (1 << bit);
            else
                module.StateMask &= ~(1 << bit);

            //string ev = $"{module.Id} state[{module.StateMask}]";
            string ev = $"{module.Id} state[0x{module.StateMask:X}]";

            Console.WriteLine($"[HW-FEEDBACK] {ev}");

            // MUST HAVE: send to client
            var writer = _currentWriter;
            if (writer == null)
            {
                Console.WriteLine("!! No writer available for EVENT");
                return;
            }

            await WriteEventAsync(writer, ev);
        }

        //public async Task OnSensorChangedAsync(int sensorId, bool occupied)
        //{
        //    // We expect sensorId 1..N (1 = module 1, port 1 => ECoS module 100, bit 0)
        //    if (sensorId <= 0)
        //        return;

        //    // Map global sensorId to ECoS feedback module + bit index
        //    //  - First module has ID 100 (see ESU docs)
        //    //  - 16 ports per module
        //    int moduleIndex = (sensorId - 1) / 16;   // 0-based module number
        //    int portIndex = (sensorId - 1) % 16;   // 0..15 within the module
        //    int moduleId = 100 + moduleIndex;     // 100, 101, 102, ...

        //    int bit = 1 << portIndex;

        //    int newMask;
        //    lock (_sensorStateLock)
        //    {
        //        _sensorModuleStates.TryGetValue(moduleId, out var mask);

        //        if (occupied)
        //            mask |= bit;   // set bit
        //        else
        //            mask &= ~bit;  // clear bit

        //        _sensorModuleStates[moduleId] = mask;
        //        newMask = mask;
        //    }

        //    // Build the ECoS-style event line, e.g. "100 state[1]" or "100 state[0]"
        //    string ev = $"{moduleId} state[{newMask}]";

        //    Console.WriteLine($"[HW-FEEDBACK] {ev}");

        //    var handler = EventGenerated;
        //    if (handler != null)
        //        await handler(ev);
        //}

        /// <summary>
        /// Handles a switch command by updating the state of a hardware switch and generating corresponding events.
        /// </summary>
        /// <remarks>This method processes commands to control hardware switches. Supported commands
        /// include: - **Switch Commands**: Commands in the format "switch[...]" are used to set the state of a specific
        /// switch. - **State Commands**: Commands in the format "state[...]" are used to set the state of a switch
        /// based on its output index.  The method validates the command format, updates the hardware switch state, and
        /// enqueues an event to reflect the change. Invalid or unrecognized commands are ignored.</remarks>
        /// <param name="ecosId">The identifier of the ecosystem component associated with the switch command.</param>
        /// <param name="option">The command option specifying the switch operation. Expected formats include "switch[...]" or "state[...]".</param>
        /// <param name="events">A list to which any generated events will be added. This list is updated with events reflecting the new
        /// switch state.</param>
        private void HandleSwitchCommand(int ecosId, string option, List<string> events)
        {
            Console.WriteLine($"[SET SWITCH] ecosId={ecosId}, opt={option}");

            // Example: set(11, switch[3g]) or set(11, switch[MOT3g])
            if (option.StartsWith("switch", StringComparison.OrdinalIgnoreCase))
            {
                var (ok, inner) = ParseBracketStringRaw(option);
                if (!ok || string.IsNullOrWhiteSpace(inner))
                    return;

                // inner is for example "3g" or "MOT3g"
                var m = Regex.Match(inner, @"^(?:MOT)?(?<addr>\d+)(?<side>[gr])$", RegexOptions.IgnoreCase);
                if (!m.Success)
                    return;

                int decoderAddress = int.Parse(m.Groups["addr"].Value);
                char side = char.ToLowerInvariant(m.Groups["side"].Value[0]);

                int outputIndex = side == 'g' ? 0 : 1;
                bool on = true;

                _hardware.SetSwitch(decoderAddress, outputIndex, on);

                // Update internal state and enqueue an event for Koploper.
                var ev = UpdateSwitchStateAndCreateEvent(ecosId, decoderAddress, outputIndex);
                events.Add(ev);
                return;
            }

            // Example: set(11, state[0])
            if (option.StartsWith("state", StringComparison.OrdinalIgnoreCase))
            {
                var (ok, value) = ParseBracketInt(option);
                if (!ok)
                    return;

                int outputIndex = value switch
                {
                    0 => 0,
                    1 => 1,
                    _ => 0
                };

                int decoderAddress = ecosId; // simple 1:1 mapping for now

                _hardware.SetSwitch(decoderAddress, outputIndex, true);

                var ev = UpdateSwitchStateAndCreateEvent(ecosId, decoderAddress, outputIndex);
                events.Add(ev);
            }
        }
        //private void HandleSwitchCommand(int ecosId, string option)
        //{
        //    Console.WriteLine($"[SET SWITCH] ecosId={ecosId}, opt={option}");

        //    // Voorbeeld 1: set(11, switch[MOT11g]) of set(11, switch[11g])
        //    if (option.StartsWith("switch", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var (ok, inner) = ParseBracketStringRaw(option);
        //        if (!ok || string.IsNullOrWhiteSpace(inner))
        //            return;

        //        // inner is bijv. "MOT11g" of "11g"
        //        // Regex: optioneel "MOT", dan getal, dan g/r
        //        var m = Regex.Match(inner, @"^(?:MOT)?(?<addr>\d+)(?<side>[gr])$", RegexOptions.IgnoreCase);
        //        if (!m.Success)
        //            return;

        //        int decoderAddress = int.Parse(m.Groups["addr"].Value);
        //        char side = char.ToLowerInvariant(m.Groups["side"].Value[0]);

        //        int outputIndex = side == 'g' ? 0 : 1;
        //        bool on = true; // bij een korte puls kun je in je echte backend eventueel zelf timen

        //        _hardware.SetSwitch(decoderAddress, outputIndex, on);
        //        return;
        //    }

        //    // Voorbeeld 2: set(11, state[0])  (0 = recht, 1 = afbuigend, 2 = etc. bij 3-weg)
        //    if (option.StartsWith("state", StringComparison.OrdinalIgnoreCase))
        //    {
        //        var (ok, value) = ParseBracketInt(option);
        //        if (!ok)
        //            return;

        //        // Heel eenvoudige mapping:
        //        // 0 => uitgang g
        //        // 1 => uitgang r
        //        // (2 en hoger kun je later uitbreiden als je 3-weg / complex spul wilt ondersteunen)
        //        int outputIndex = value switch
        //        {
        //            0 => 0,
        //            1 => 1,
        //            _ => 0
        //        };

        //        // Voorlopig mappen we ecosId -> decoderadres één-op-één
        //        int decoderAddress = ecosId;

        //        _hardware.SetSwitch(decoderAddress, outputIndex, true);
        //    }
        //}

        /// <summary>
        /// Parses an integer value enclosed in square brackets from the specified string.
        /// </summary>
        /// <remarks>The method searches for the first occurrence of square brackets ('[' and ']') in the
        /// input string. If the content between the brackets can be parsed as an integer, the method returns <see
        /// langword="true"/>  along with the parsed value. Otherwise, it returns <see langword="false"/> and a default
        /// value of 0.</remarks>
        /// <param name="option">The input string containing the square-bracketed integer value to parse.</param>
        /// <returns>A tuple containing a boolean and an integer. The boolean indicates whether the parsing was successful. If
        /// successful, the integer represents the parsed value; otherwise, it is 0.</returns>
        private static (bool ok, int value) ParseBracketInt(string option)
        {
            int start = option.IndexOf('[');
            int end = option.IndexOf(']');
            if (start > 0 && end > start &&
                int.TryParse(option[(start + 1)..end], out int value))
            {
                return (true, value);
            }
            return (false, 0);
        }

        /// <summary>
        /// Parses a string to extract the content enclosed within square brackets ('[' and ']').
        /// </summary>
        /// <remarks>If the content within the brackets is enclosed in double quotes, the quotes are
        /// removed from the result. The method returns <see langword="false"/> if the input does not contain valid
        /// square brackets or if the brackets are improperly ordered.</remarks>
        /// <param name="option">The input string to parse. Must contain square brackets to extract content.</param>
        /// <returns>A tuple containing a boolean and a string. The boolean indicates whether the parsing was successful. If
        /// successful, the string contains the extracted content; otherwise, it is an empty string.</returns>
        private static (bool ok, string value) ParseBracketString(string option)
        {
            int start = option.IndexOf('[');
            int end = option.LastIndexOf(']');
            if (start > 0 && end > start)
            {
                var inner = option[(start + 1)..end];

                if (inner.Length >= 2 && inner[0] == '"' && inner[^1] == '"')
                {
                    inner = inner[1..^1];
                }

                return (true, inner);
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// Attempts to extract the content enclosed within square brackets ('[' and ']') from the specified string.
        /// </summary>
        /// <param name="option">The input string to parse for bracket-enclosed content.</param>
        /// <returns>A tuple containing a boolean and a string. The boolean is <see langword="true"/> if the input string
        /// contains valid bracket-enclosed content; otherwise, <see langword="false"/>. The string is the extracted
        /// content if successful, or an empty string if the operation fails.</returns>
        private static (bool ok, string value) ParseBracketStringRaw(string option)
        {
            int start = option.IndexOf('[');
            int end = option.LastIndexOf(']');
            if (start < 0 || end <= start)
                return (false, string.Empty);

            string inner = option.Substring(start + 1, end - start - 1);
            return (true, inner);
        }

        /// <summary>
        /// Parses a function option like:
        ///   func[0][1]
        ///   func[1][0]
        ///   func[2]=1
        /// Returns: (ok, index, value)
        /// </summary>
        private static (bool ok, int index, int value) ParseFunctionOption(string option)
        {
            if (!option.StartsWith("func", StringComparison.OrdinalIgnoreCase))
                return (false, 0, 0);

            // Find first '[' and ']' → function index
            int firstOpen = option.IndexOf('[');
            int firstClose = option.IndexOf(']', firstOpen + 1);
            if (firstOpen < 0 || firstClose <= firstOpen)
                return (false, 0, 0);

            string indexText = option.Substring(firstOpen + 1, firstClose - firstOpen - 1);
            if (!int.TryParse(indexText, out int index))
                return (false, 0, 0);

            // Try second [value] form: func[0][1]
            int secondOpen = option.IndexOf('[', firstClose + 1);
            if (secondOpen >= 0)
            {
                int secondClose = option.IndexOf(']', secondOpen + 1);
                if (secondClose > secondOpen)
                {
                    string valueText = option.Substring(secondOpen + 1, secondClose - secondOpen - 1);
                    if (int.TryParse(valueText, out int value))
                    {
                        return (true, index, value);
                    }
                }
            }

            // Fallback: func[0]=1 or func[0]=0
            int eqPos = option.IndexOf('=', firstClose + 1);
            if (eqPos >= 0 && eqPos + 1 < option.Length)
            {
                string valueText = option.Substring(eqPos + 1).Trim();
                if (int.TryParse(valueText, out int value))
                {
                    return (true, index, value);
                }
            }

            return (false, index, 0);
        }

        /// <summary>
        /// Updates the internal switch state and returns the ECoS event line to be sent to clients.
        /// This is used both for commands coming from Koploper and for external C# code.
        /// </summary>
        private string UpdateSwitchStateAndCreateEvent(int ecosId, int decoderAddress, int outputIndex)
        {
            lock (_switchStateLock)
            {
                if (!_switchStates.TryGetValue(ecosId, out var state))
                {
                    state = new SwitchState
                    {
                        EcosId = ecosId,
                        DecoderAddress = decoderAddress,
                        CurrentOutputIndex = outputIndex
                    };

                    _switchStates[ecosId] = state;
                }
                else
                {
                    state.DecoderAddress = decoderAddress;
                    state.CurrentOutputIndex = outputIndex;
                }
            }

            // ECoS convention: state[0] = straight, state[1] = diverging.
            return $"{ecosId} state[{outputIndex}]";
        }

        /// <summary>
        /// Writes a formatted reply message to the specified <see cref="TextWriter"/> asynchronously.
        /// </summary>
        /// <remarks>The reply message consists of a header, an optional body, and a footer: <list
        /// type="bullet"> <item> <description>The header is formatted as <c>&lt;REPLY {cmdText}&gt;</c>.</description>
        /// </item> <item> <description>The body contains the lines provided in <paramref name="lines"/>, if
        /// any.</description> </item> <item> <description>The footer is formatted as <c>&lt;END {errorCode}
        /// ({errorText})&gt;</c>.</description> </item> </list> If an <see cref="IOException"/> or <see
        /// cref="ObjectDisposedException"/> occurs during the write operation, the exception is logged, and the method
        /// exits gracefully.</remarks>
        /// <param name="writer">The <see cref="TextWriter"/> to which the reply message will be written. Cannot be <c>null</c>.</param>
        /// <param name="cmdText">The command text to include in the reply header.</param>
        /// <param name="errorCode">The error code to include in the reply footer.</param>
        /// <param name="errorText">The error description to include in the reply footer.</param>
        /// <param name="lines">An optional collection of lines to include in the body of the reply. If <c>null</c>, no body lines will be
        /// written.</param>
        /// <returns></returns>
        private static async Task WriteReplyAsync(TextWriter writer,string cmdText, int errorCode, string errorText, IEnumerable<string>? lines)
        {
            string header = $"<REPLY {cmdText}>";
            string end = $"<END {errorCode} ({errorText})>";

            try
            {
                Console.WriteLine("TX: " + header);
                await writer.WriteLineAsync(header);

                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        Console.WriteLine("TX: " + line);
                        await writer.WriteLineAsync(line);
                    }
                }

                Console.WriteLine("TX: " + end);
                await writer.WriteLineAsync(end);

                await writer.FlushAsync();
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("  !! IO-exception during TX (client disconnect?): " + ioEx.Message);
            }
            catch (ObjectDisposedException)
            {
                // stream/writer al dicht, niets meer aan doen
                Console.WriteLine("  !! Writer/stream already closed at TX.");
            }                        
        }

        /// <summary>
        /// Sends an unsolicited ECoS event to the connected client.
        /// </summary>
        /// <param name="writer">The TextWriter associated with the client connection.</param>
        /// <param name="eventLine">
        /// The raw event payload, for example: "11 state[0]" or "1000 speed[40]".
        /// </param>
        private static async Task WriteEventAsync(TextWriter writer, string eventPayload)
        {
            if (writer == null)
                return;

            // eventPayload is bijvoorbeeld: "100 state[1]" of "11 state[0]"
            // Eerste token vóór de spatie is de object-id.
            string idPart = eventPayload;
            int spaceIndex = eventPayload.IndexOf(' ');
            if (spaceIndex > 0)
                idPart = eventPayload.Substring(0, spaceIndex);

            // 1) EVENT header
            string header = $"<EVENT {idPart}>";
            Console.WriteLine("TX: " + header);
            await writer.WriteLineAsync(header);

            // 2) Payload regel
            Console.WriteLine("TX: " + eventPayload);
            await writer.WriteLineAsync(eventPayload);

            // 3) END-regel
            const string endLine = "<END 0 (OK)>";
            Console.WriteLine("TX: " + endLine);
            await writer.WriteLineAsync(endLine);

            await writer.FlushAsync();
        }
        //private static async Task WriteEventAsync(TextWriter writer, string eventLine)
        //{
        //    string line = $"<EVENT {eventLine}>";

        //    try
        //    {
        //        Console.WriteLine("TX: " + line);
        //        await writer.WriteLineAsync(line);
        //        await writer.FlushAsync();
        //    }
        //    catch (IOException ioEx)
        //    {
        //        Console.WriteLine("  !! IO-exception during EVENT TX (client disconnect?): " + ioEx.Message);
        //    }
        //    catch (ObjectDisposedException)
        //    {
        //        Console.WriteLine("  !! Writer/stream already closed at EVENT TX.");
        //    }
        //}

        /// <summary>
        /// Represents the state of a locomotive, including its identifier, address, speed, direction, and optional
        /// block information.
        /// </summary>
        /// <remarks>This class is used to encapsulate the current state of a locomotive, including its
        /// unique identifier, operational parameters,  and the last known block number, if available. The <see
        /// cref="Direction"/> property uses 1 to indicate forward and -1 to indicate reverse.</remarks>
        private sealed class LocoState
        {
            public int EcosId { get; set; }
            public int Address { get; set; }

            public int Speed { get; set; }
            public int Direction { get; set; } = 1;

            /// <summary>
            /// Last known block number for this locomotive (optional).
            /// </summary>
            public int? Block { get; set; }

            /// <summary>
            /// Function states (F0..F28). Index = function number.
            /// </summary>
            public bool[] Functions { get; } = new bool[29];

            public bool SetFunction(int index, bool newState)
            {
                if (index < 0 || index >= Functions.Length)
                    return false;

                bool changed = Functions[index] != newState;
                Functions[index] = newState;
                return changed;
            }

            public void LoadFunctionsFromInfo(LocoInfo info)
            {
                for (int i = 0; i < Functions.Length && i < info.Functions.Length; i++)
                    Functions[i] = info.Functions[i] != 0;
            }

            public void SaveFunctionsToInfo(LocoInfo info)
            {
                for (int i = 0; i < Functions.Length && i < info.Functions.Length; i++)
                    info.Functions[i] = Functions[i] ? 1 : 0;
            }
        }

        // Stores logical switch state per ECoS object id.
        // For now we assume 1:1 mapping: ecosId == logical switch id.
        private sealed class SwitchState
        {
            public int EcosId { get; init; }
            public int DecoderAddress { get; set; }
            public int CurrentOutputIndex { get; set; } // 0 = straight, 1 = diverging
        }

        /// <summary>
        /// Represents a feedback module with a unique identifier, a configurable number of input ports,  and a state
        /// mask indicating the occupancy of those ports.
        /// </summary>
        /// <remarks>This class is designed to encapsulate the state and configuration of a feedback
        /// module.  The <see cref="Ports"/> property specifies the number of input ports available, while the  <see
        /// cref="StateMask"/> property uses a bitmask to represent the occupancy of each port.</remarks>
        private sealed class FeedbackModule
        {
            public int Id { get; init; }

            /// <summary>
            /// Number of input ports on this module (typically 16).
            /// </summary>
            public int Ports { get; init; }

            /// <summary>
            /// Bit mask of occupied ports. Bit 0 = port 1, bit 1 = port 2, etc.
            /// </summary>
            public int StateMask { get; set; }
        }
    }
}
