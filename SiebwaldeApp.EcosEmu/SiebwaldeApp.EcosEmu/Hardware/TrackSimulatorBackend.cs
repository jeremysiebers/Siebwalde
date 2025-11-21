using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Simuleert een baan met blokken, treinen en bezetmelders.
    /// </summary>
    public class TrackSimulatorBackend : IHardwareBackend
    {
        private readonly object _lock = new();
        private readonly Dictionary<int, LocoSimState> _locos = new();      // key = decoder adres
        private readonly Dictionary<int, BlockDef> _blocks = new();        // key = BlockId
        private readonly IBlockPositionProvider _blockPositionProvider;

        private IHardwareFeedbackSink? _feedbackSink;
        private CancellationTokenSource? _simCts;
        private Task? _simTask;
        private bool _powerOn;
        
        public TrackSimulatorBackend(IBlockPositionProvider blockPositionProvider)
        {
            _blockPositionProvider = blockPositionProvider ?? throw new ArgumentNullException(nameof(blockPositionProvider));
            // Listen for Koploper updates.
            _blockPositionProvider.BlockEntered += OnKoploperLocBlockChanged;
            InitSimpleLoop();
        }

        /// <summary>
        /// Koppeling naar de ECoS-backend zodat we sensor-events kunnen teruggeven.
        /// (zelfde patroon als DummyHardwareBackend)
        /// </summary>
        public void AttachFeedbackSink(IHardwareFeedbackSink sink)
        {
            _feedbackSink = sink ?? throw new ArgumentNullException(nameof(sink));
            StartSimulationLoop();
        }

        // ========== IHardwareBackend ==========

        public void SetPower(bool on)
        {
            Console.WriteLine($"[SIM-HW] Power {(on ? "ON" : "OFF")}");

            lock (_lock)
            {
                _powerOn = on;
                if (!on)
                {
                    foreach (var loco in _locos.Values)
                        loco.SpeedSteps = 0;
                }
            }
        }

        private void OnKoploperLocBlockChanged(int locoId, int blockId)
        {
            lock (_lock)
            {
                // 1. Loco removed from layout (Block = 0)
                if (blockId <= 0)
                {
                    if (_locos.TryGetValue(locoId, out var loco))
                    {
                        // Clear old block occupancy
                        if (_blocks.TryGetValue(loco.BlockId, out var oldBlock) &&
                            _feedbackSink != null &&
                            oldBlock.EnterSensorId != 0)
                        {
                            _ = _feedbackSink.OnSensorChangedAsync(oldBlock.EnterSensorId, false);
                        }

                        _locos.Remove(locoId);
                    }

                    return;
                }

                // 2. Loco is placed or moved to a real block
                if (!_blocks.TryGetValue(blockId, out var newBlock))
                {
                    Console.WriteLine($"[SIM] Koploper reports block {blockId}, but simulator does not know this block.");
                    return;
                }

                // Clear previous occupancy
                if (_locos.TryGetValue(locoId, out var existing) &&
                    _blocks.TryGetValue(existing.BlockId, out var previousBlock) &&
                    _feedbackSink != null &&
                    previousBlock.EnterSensorId != 0)
                {
                    _ = _feedbackSink.OnSensorChangedAsync(previousBlock.EnterSensorId, false);
                }

                // Place loco on new block
                var locoSim = _locos.TryGetValue(locoId, out var locEntry)
                    ? locEntry
                    : new LocoSimState { DecoderAddress = locoId };

                locoSim.BlockId = blockId;
                locoSim.PositionMm = 0;
                _locos[locoId] = locoSim;

                // Mark new block as occupied
                if (_feedbackSink != null && newBlock.EnterSensorId != 0)
                {
                    _ = _feedbackSink.OnSensorChangedAsync(newBlock.EnterSensorId, true);
                }
            }
        }

        public void SetLocoSpeed(int decoderAddress, int speedSteps, int direction)
        {
            Console.WriteLine($"[SIM-HW] Loco addr={decoderAddress} speed={speedSteps} dir={direction}");

            lock (_lock)
            {
                if (!_locos.TryGetValue(decoderAddress, out var loco))
                {
                    // Ask Koploper for last known block (>0)
                    int? blockFromKoploper = _blockPositionProvider.TryGetBlockForLoc(decoderAddress);

                    if (blockFromKoploper == null || blockFromKoploper <= 0)
                    {
                        // Locomotive is NOT on the layout, so ignore.
                        return;
                    }

                    if (!_blocks.TryGetValue(blockFromKoploper.Value, out _))
                    {
                        Console.WriteLine($"[SIM-HW] Unknown block {blockFromKoploper} for loco {decoderAddress}.");
                        return;
                    }

                    // ONLY create sim state, do NOT touch occupancy here.
                    loco = new LocoSimState
                    {
                        DecoderAddress = decoderAddress,
                        BlockId = blockFromKoploper.Value,
                        PositionMm = 0,
                        Direction = 1,
                        SpeedSteps = 0
                    };

                    _locos[decoderAddress] = loco;
                }

                // Update movement only
                loco.Direction = direction >= 0 ? 1 : -1;
                loco.SpeedSteps = Math.Max(0, speedSteps);
            }
        }




        public void SetSwitch(int decoderAddress, int outputIndex, bool on)
        {
            // Voor nu negeren we wissels in de sim (blokvolgorde is vast).
            Console.WriteLine($"[SIM-HW] Switch addr={decoderAddress} out={outputIndex} on={on}");
        }

        // ========== Simulatie-loop ==========

        private void StartSimulationLoop()
        {
            if (_simTask != null)
                return;

            _simCts = new CancellationTokenSource();
            _simTask = Task.Run(() => SimLoopAsync(_simCts.Token));
        }

        private async Task SimLoopAsync(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            double lastMs = sw.Elapsed.TotalMilliseconds;

            while (!ct.IsCancellationRequested)
            {
                var now = sw.Elapsed.TotalMilliseconds;
                var dtMs = now - lastMs;
                lastMs = now;

                StepSimulation(dtMs / 1000.0); // seconden

                await Task.Delay(50, ct).ConfigureAwait(false); // ~20 Hz
            }
        }

        private void StepSimulation(double dtSeconds)
        {
            if (!_powerOn || _feedbackSink == null)
                return;

            List<(int sensorId, bool occupied)> sensorEvents = new();

            lock (_lock)
            {
                foreach (var loco in _locos.Values)
                {
                    if (loco.SpeedSteps <= 0)
                        continue;

                    if (!_blocks.TryGetValue(loco.BlockId, out var block))
                        continue;

                    // Super simpel: 1 speedstep = 10 mm/s
                    double mmPerSecondPerStep = 10.0;
                    double v = loco.SpeedSteps * mmPerSecondPerStep * loco.Direction;
                    double oldPos = loco.PositionMm;
                    double newPos = oldPos + v * dtSeconds;

                    // Binnen blok gebleven?
                    if (newPos >= 0 && newPos <= block.LengthMm)
                    {
                        loco.PositionMm = newPos;
                        continue;
                    }

                    // Blok verlaten: exit-sensor vrij (als die bestaat)
                    if (block.ExitSensorId != 0)
                    {
                        sensorEvents.Add((block.ExitSensorId, false));
                    }

                    // Volgend blok bepalen
                    int? nextBlockId = loco.Direction > 0
                        ? block.NextBlockForward
                        : block.NextBlockReverse;

                    if (nextBlockId == null || !_blocks.TryGetValue(nextBlockId.Value, out var nextBlock))
                    {
                        // Geen volgend blok → trein stopt
                        loco.SpeedSteps = 0;
                        continue;
                    }

                    loco.BlockId = nextBlock.BlockId;
                    loco.PositionMm = loco.Direction > 0 ? 0 : nextBlock.LengthMm;

                    // Inrijden in volgend blok → enter-sensor bezet
                    if (nextBlock.EnterSensorId != 0)
                    {
                        sensorEvents.Add((nextBlock.EnterSensorId, true));
                    }
                }
            }

            // Buiten de lock: async callbacks naar backend
            foreach (var (sensorId, occ) in sensorEvents)
            {
                _ = _feedbackSink!.OnSensorChangedAsync(sensorId, occ);
            }
        }

        /// <summary>
        /// Voor nu: 4 blokken in een rondje, 1 sensor per blok (1..4).
        /// </summary>
        private void InitSimpleLoop()
        {
            // 1 sensor per block: same ID for Enter and Exit
            _blocks[1] = new BlockDef(
                BlockId: 1,
                EnterSensorId: 1,
                ExitSensorId: 1,
                LengthMm: 1000,
                NextBlockForward: 2,
                NextBlockReverse: 4);

            _blocks[2] = new BlockDef(
                BlockId: 2,
                EnterSensorId: 2,
                ExitSensorId: 2,
                LengthMm: 1000,
                NextBlockForward: 3,
                NextBlockReverse: 1);

            _blocks[3] = new BlockDef(
                BlockId: 3,
                EnterSensorId: 3,
                ExitSensorId: 3,
                LengthMm: 1000,
                NextBlockForward: 4,
                NextBlockReverse: 2);

            _blocks[4] = new BlockDef(
                BlockId: 4,
                EnterSensorId: 4,
                ExitSensorId: 4,
                LengthMm: 1000,
                NextBlockForward: 1,
                NextBlockReverse: 3);
        }

        // ========== Hulp-types ==========

        private sealed class LocoSimState
        {
            public int DecoderAddress { get; set; }
            public int BlockId { get; set; }
            public double PositionMm { get; set; }
            public int Direction { get; set; }
            public int SpeedSteps { get; set; }
        }

        private sealed record BlockDef(
            int BlockId,
            int EnterSensorId,
            int ExitSensorId,
            double LengthMm,
            int? NextBlockForward,
            int? NextBlockReverse);
    }
}
