using System.Linq;
namespace SiebwaldeApp.EcosEmu
{
    public class SimpleEcosBackend : IEcosBackend
    {        
        private readonly Dictionary<int, LocoState> _locos = new();
        private readonly IHardwareBackend _hardware;
        private readonly ILocoRepository _locoRepository;
        private readonly IBlockPositionProvider _blockPositionProvider;
                
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

        //private void OnBlockEntered(int locoAddress, int blockNumber)
        //{
        //    // Update runtime loco state
        //    var matchingState = _locos.Values.FirstOrDefault(l => l.Address == locoAddress);
        //    if (matchingState != null)
        //    {
        //        matchingState.Block = blockNumber;
        //    }

        //    // Update persistent loco info
        //    var info = _locoRepository.GetByAddress(locoAddress);
        //    if (info != null)
        //    {
        //        info.Block = blockNumber;
        //        _locoRepository.AddOrUpdate(info);

        //        // Fire-and-forget save to avoid blocking the external callback
        //        _ = _locoRepository.SaveAsync();
        //    }

        //    Console.WriteLine($"[LOCO] Address {locoAddress} is now in block {blockNumber}.");
        //}

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
                default:
                    await WriteReplyAsync(writer, cmd.RawLine, 1, "UNKNOWN_COMMAND", null);
                    break;
            }
        }

        private async Task HandleRequestAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // Voor nu heel simpel: accepteer alles.
            // request(1,view)
            // request(10,view)
            // Koploper verwacht alleen "OK" en evt. events later.

            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
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

            int id = cmd.ObjectId.Value;

            // Basisobject ECoS: set(1, go/stop)
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
                // Try to resolve the address from the persistent repository first
                var info = _locoRepository.GetByEcosId(id);
                int address = info?.Address ?? id;

                loco = new LocoState
                {
                    EcosId = id,
                    Address = address,
                    Block = info?.Block
                };

                _locos[id] = loco;
            }

            foreach (var opt in cmd.Options)
            {
                if (opt.StartsWith("speed", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, value) = ParseBracketInt(opt);
                    if (ok)
                    {
                        loco.Speed = value;
                        _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);
                    }
                }
                else if (opt.StartsWith("dir", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, value) = ParseBracketInt(opt);
                    if (ok)
                    {
                        loco.Direction = value;
                        _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);
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
                        info.Block = loco.Block; // keep current block if known
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
                // TODO: handle addrext[...] or protocol[...] if Koploper sends them
            }

            //foreach (var opt in cmd.Options)
            //{
            //    if (opt.StartsWith("speed", StringComparison.OrdinalIgnoreCase))
            //    {
            //        var (ok, value) = ParseBracketInt(opt);
            //        if (ok)
            //        {
            //            loco.Speed = value;
            //            _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);
            //        }
            //    }
            //    else if (opt.StartsWith("dir", StringComparison.OrdinalIgnoreCase))
            //    {
            //        var (ok, value) = ParseBracketInt(opt);
            //        if (ok)
            //        {
            //            loco.Direction = value;
            //            _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);
            //        }
            //    }
            //    else if (opt.StartsWith("addr", StringComparison.OrdinalIgnoreCase))
            //    {
            //        var (ok, value) = ParseBracketInt(opt);
            //        if (ok)
            //        {
            //            loco.Address = value;

            //            // Update persistent loco info as well
            //            var info = _locoRepository.GetByEcosId(id) ?? new LocoInfo { EcosId = id };
            //            info.Address = value;
            //            _locoRepository.AddOrUpdate(info);
            //            await _locoRepository.SaveAsync(ct);
            //        }
            //    }
            //    else if (opt.StartsWith("name", StringComparison.OrdinalIgnoreCase))
            //    {
            //        var (ok, text) = ParseBracketString(opt);
            //        if (ok)
            //        {
            //            var info = _locoRepository.GetByEcosId(id) ?? new LocoInfo { EcosId = id };
            //            info.Name = text;
            //            _locoRepository.AddOrUpdate(info);
            //            await _locoRepository.SaveAsync(ct);
            //        }
            //    }
            //    // addrext / protocol etc. can be added here in a similar way later
            //}

            //foreach (var opt in cmd.Options)
            //{
            //    if (opt.StartsWith("speed", StringComparison.OrdinalIgnoreCase))
            //    {
            //        var (ok, value) = ParseBracketInt(opt);
            //        if (ok)
            //        {
            //            loco.Speed = value;
            //            _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);
            //        }
            //    }
            //    else if (opt.StartsWith("dir", StringComparison.OrdinalIgnoreCase))
            //    {
            //        var (ok, value) = ParseBracketInt(opt);
            //        if (ok)
            //        {
            //            loco.Direction = value;
            //            _hardware.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction);
            //        }
            //    }
            //}

            var lines = new List<string>
        {
            $"{id} speed[{loco.Speed}]",
            $"{id} dir[{loco.Direction}]"
        };

            await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
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

        //private async Task HandleQueryObjectsAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        //{
        //    // Als LokManager (ID=10) → lijst met loc-IDs
        //    if (cmd.ObjectId == 10)
        //    {
        //        //var lines = _locos.Keys
        //        //    .OrderBy(k => k)
        //        //    .Select(id => $"{id}")
        //        //    .ToList();
        //        var lines = new List<string>
        //        {
        //            //"1000 addr[3] name[\"TestLok 3\"] protocol[\"DCC28\"]",
        //            //"1001 addr[4] name[\"TestLok 4\"] protocol[\"DCC28\"]"
        //        };

        //        await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", lines);
        //    }
        //    else
        //    {
        //        await WriteReplyAsync(writer, cmd.RawLine, 0, "OK", null);
        //    }
        //}

        private async Task HandleQueryObjectsAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
        {
            // LokManager (ID = 10) → list of locomotive definitions
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

                    // Format is intentionally very close to your previous hard coded version.
                    // Example:
                    // 1000 addr[3] name["TestLok 3"] protocol["DCC28"]
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

        private static (bool ok, string value) ParseBracketString(string option)
        {
            int start = option.IndexOf('[');
            int end = option.LastIndexOf(']');
            if (start > 0 && end > start)
            {
                var inner = option[(start + 1)..end];

                // Remove surrounding quotes if present
                if (inner.Length >= 2 && inner[0] == '"' && inner[^1] == '"')
                {
                    inner = inner[1..^1];
                }

                return (true, inner);
            }

            return (false, string.Empty);
        }

        private static async Task WriteReplyAsync(
            TextWriter writer,
            string cmdText,
            int errorCode,
            string errorText,
            IEnumerable<string>? lines)
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
        }
    }
}
