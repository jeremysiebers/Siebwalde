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

        /// <summary>
        /// Raised whenever an ECoS-style event line should be sent to connected clients.
        /// The string is the raw event payload, e.g. "11 state[0]".
        /// </summary>
        public event Func<string, Task>? EventGenerated;

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

        private async Task HandleRequestAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // Example: request(1000,control, force) or request(1000,view)

            if (cmd.ObjectId == null)
            {
                await WriteReplyAsync(writer, cmd.RawLine, 2, "MISSING_ID", null);
                return;
            }

            int id = (int)cmd.ObjectId;

            if (!_locos.TryGetValue(id, out var loco))
            {
                // Try to create state from repository if not present yet
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

                    // Load function states from JSON if available
                    loco.LoadFunctionsFromInfo(info);

                    _locos[info.EcosId] = loco;
                }
            }

            var lines = new List<string>();

            if (loco != null)
            {
                // Always report speed and direction
                lines.Add($"{id} speed[{loco.Speed}]");
                lines.Add($"{id} dir[{loco.Direction}]");

                // IMPORTANT:
                // Do not report all function states here, because Koploper appears
                // to crash when func[...] lines are returned as part of a request(...)
                // reply at startup.
                //
                // The function states are still kept in memory and JSON and will
                // be reflected via func[...] events when Koploper changes them.
                //
                //for (int i = 0; i < loco.Functions.Length; i++)
                //{
                //    int val = loco.Functions[i] ? 1 : 0;
                //    lines.Add($"{id} func[{i}][{val}]");
                //}
            }

            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
        }

        private async Task HandleReleaseAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // Voor nu heel simpel: accepteer alles.
            // request(1,view)
            // request(10,view)
            // Koploper verwacht alleen "OK" en evt. events later.

            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
        }

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

        private async Task HandleGetAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            if (cmd.ObjectId == null)
            {
                await WriteReplyAsync(writer, cmd.RawLine, 2, "MISSING_ID", null);
                return;
            }

            int id = cmd.ObjectId.Value;

            // Special case: basisobject ECoS info
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

                    // Example:
                    // 1000 addr[3] addrext[0] name["TestLok 3"] protocol["DCC28"]
                    string line =
                        $"{loco.EcosId} " +
                        $"addr[{loco.Address}] " +
                        $"addrext[{addrext}] " +
                        $"name[\"{safeName}\"] " +
                        $"protocol[\"{protocol}\"]";

                    lines.Add(line);
                }

                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
            }
            else
            {
                await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
            }
        }

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

        public async Task OnSwitchChangedAsync(int ecosId, int decoderAddress, int outputIndex)
        {
            // Update internal state and build the event line
            var ev = UpdateSwitchStateAndCreateEvent(ecosId, decoderAddress, outputIndex);

            Console.WriteLine($"[HW-FEEDBACK] {ev}");

            // Let the TCP layer decide how to send this event to the client(s)
            var handler = EventGenerated;
            if (handler != null)
            {
                await handler(ev);
            }
        }

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
        private static async Task WriteEventAsync(TextWriter writer, string eventLine)
        {
            string line = $"<EVENT {eventLine}>";

            try
            {
                Console.WriteLine("TX: " + line);
                await writer.WriteLineAsync(line);
                await writer.FlushAsync();
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("  !! IO-exception during EVENT TX (client disconnect?): " + ioEx.Message);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("  !! Writer/stream already closed at EVENT TX.");
            }
        }

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
    }
}
