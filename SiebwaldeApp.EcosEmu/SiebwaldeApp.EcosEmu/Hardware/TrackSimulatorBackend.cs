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
        private readonly Dictionary<(int decoder, int outputIndex), bool> _switchStates = new();


        private IHardwareFeedbackSink? _feedbackSink;
        private CancellationTokenSource? _simCts;
        private Task? _simTask;
        private bool _powerOn;
        
        public TrackSimulatorBackend(IBlockPositionProvider blockPositionProvider)
        {
            _blockPositionProvider = blockPositionProvider ?? throw new ArgumentNullException(nameof(blockPositionProvider));
            // Listen for Koploper updates.
            _blockPositionProvider.BlockEntered += OnKoploperLocBlockChanged;
            InitOvalWithLadder();
        }

        /// <summary>
        /// Koppeling naar de ECoS-backend zodat we sensor-events kunnen teruggeven.
        /// (zelfde patroon als DummyHardwareBackend)
        /// </summary>
        public void AttachFeedbackSink(IHardwareFeedbackSink sink)
        {
            _feedbackSink = sink ?? throw new ArgumentNullException(nameof(sink));

            // Eerst alles schoonzetten
            ResetAllSensors();

            StartSimulationLoop();
        }

        // ========== IHardwareBackend ==========
        /// <summary>
        /// Sets the power state of the hardware.
        /// </summary>
        /// <remarks>When the power is turned off, the speed of all locomotives is set to zero.</remarks>
        /// <param name="on">A boolean value indicating the desired power state.  <see langword="true"/> to turn the power on; <see
        /// langword="false"/> to turn it off.</param>
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

        private void ResetAllSensors()
        {
            if (_feedbackSink == null)
                return;

            foreach (var block in _blocks.Values)
            {
                if (block.EnterSensorId != 0)
                {
                    _ = _feedbackSink.OnSensorChangedAsync(block.EnterSensorId, false);
                }

                if (block.ExitSensorId != 0 && block.ExitSensorId != block.EnterSensorId)
                {
                    _ = _feedbackSink.OnSensorChangedAsync(block.ExitSensorId, false);
                }
            }

            Console.WriteLine("[SIM-HW] All sensors reset to FREE.");
        }

        private void OnKoploperLocBlockChanged(int locoId, int blockId)
        {
            lock (_lock)
            {
                // 1. Loco removed from layout (Block = 0)
                if (blockId <= 0)
                {
                    bool hadLoco = _locos.TryGetValue(locoId, out var loco);

                    if (hadLoco)
                    {
                        // Clear old block occupancy
                        if (_blocks.TryGetValue(loco.BlockId, out var oldBlock) && _feedbackSink != null)
                        {
                            if (oldBlock.EnterSensorId != 0)
                            {
                                _ = _feedbackSink.OnSensorChangedAsync(oldBlock.EnterSensorId, false);
                            }

                            if (oldBlock.ExitSensorId != 0 && oldBlock.ExitSensorId != oldBlock.EnterSensorId)
                            {
                                _ = _feedbackSink.OnSensorChangedAsync(oldBlock.ExitSensorId, false);
                            }
                        }

                        _locos.Remove(locoId);
                    }

                    // Als Koploper via Recover alle locs uit de baan haalt (allemaal block 0),
                    // dan willen we ook dat ALLE sensoren als vrij gemeld worden.
                    if (_locos.Count == 0)
                    {
                        ResetAllSensors();
                    }

                    return;
                }

                // 2. Loco is placed or moved to a real block
                if (!_blocks.TryGetValue(blockId, out var newBlock))
                {
                    Console.WriteLine($"[SIM] Koploper reports block {blockId}, but simulator does not know this block.");
                    return;
                }

                // Clear previous occupancy (beide sensoren)
                if (_locos.TryGetValue(locoId, out var existing) &&
                    _blocks.TryGetValue(existing.BlockId, out var previousBlock) &&
                    _feedbackSink != null)
                {
                    if (previousBlock.EnterSensorId != 0)
                    {
                        _ = _feedbackSink.OnSensorChangedAsync(previousBlock.EnterSensorId, false);
                    }

                    if (previousBlock.ExitSensorId != 0 && previousBlock.ExitSensorId != previousBlock.EnterSensorId)
                    {
                        _ = _feedbackSink.OnSensorChangedAsync(previousBlock.ExitSensorId, false);
                    }
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

        public void SetSwitch(int decoderAddress, int outputIndex, bool on)
        {
            lock (_lock)
            {
                // Sla de toestand van deze coil op
                _switchStates[(decoderAddress, outputIndex)] = on;

                // 2-spoelen wissel: als één coil wordt geactiveerd,
                // beschouwen we de andere coil als "uit".
                // ECoS stuurt bij omschakelen alleen de "aan" coil.
                int otherIndex = outputIndex == 0 ? 1 : 0;
                _switchStates[(decoderAddress, otherIndex)] = !on;
            }

            Console.WriteLine($"[SIM-HW] Switch addr={decoderAddress} out={outputIndex} on={on}");
        }

        /// <summary>
        /// Bepaal de stand van een wissel (0 = recht, 1 = afbuigend)
        /// op basis van de twee coils (outputIndex 0 en 1).
        /// </summary>
        private int GetSwitchPosition(int decoderAddress)
        {
            bool out0 = _switchStates.TryGetValue((decoderAddress, 0), out var s0) && s0;
            bool out1 = _switchStates.TryGetValue((decoderAddress, 1), out var s1) && s1;

            // Als nog niets bekend is, ga uit van "recht" (0).
            if (out1 && !out0)
                return 1;

            return 0;
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
                        SpeedSteps = 0,
                        ExitZoneReached = false
                    };

                    _locos[decoderAddress] = loco;
                }

                // Update movement only
                loco.Direction = direction >= 0 ? 1 : -1;
                loco.SpeedSteps = Math.Max(0, speedSteps);
            }
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
            // Do not simulate if power is off or there is no feedback sink attached.
            if (!_powerOn || _feedbackSink == null)
                return;

            // Collect all sensor changes that occur in this step.
            var sensorEvents = new List<(int sensorId, bool occupied)>();

            lock (_lock)
            {
                foreach (var loco in _locos.Values)
                {
                    // Standing still → no movement, no sensor changes.
                    if (loco.SpeedSteps <= 0)
                        continue;

                    if (!_blocks.TryGetValue(loco.BlockId, out var block))
                        continue;

                    // Very simple kinematics: 1 speed step = 10 mm/s
                    const double mmPerSecondPerStep = 10.0;
                    double v = loco.SpeedSteps * mmPerSecondPerStep * loco.Direction;
                    double oldPos = loco.PositionMm;
                    double newPos = oldPos + v * dtSeconds;

                    double blockLength = block.LengthMm;
                    double midPos = blockLength * 0.5;

                    // Still inside the current block? Just update position and
                    // check whether we cross the "exit zone" (midpoint of block).
                    if (newPos >= 0 && newPos <= blockLength)
                    {
                        loco.PositionMm = newPos;

                        // Determine if we have just crossed the midpoint in this step.
                        bool crossedMid = false;
                        if (loco.Direction > 0)
                        {
                            crossedMid = oldPos < midPos && newPos >= midPos;
                        }
                        else if (loco.Direction < 0)
                        {
                            crossedMid = oldPos > midPos && newPos <= midPos;
                        }

                        // First the enter sensor (handled on block entry), then the exit sensor
                        // once we have passed the midpoint.
                        if (crossedMid &&
                            !loco.ExitZoneReached &&
                            block.ExitSensorId != 0 &&
                            block.ExitSensorId != block.EnterSensorId)
                        {
                            loco.ExitZoneReached = true;
                            sensorEvents.Add((block.ExitSensorId, true));
                        }

                        continue;
                    }

                    // Leaving the current block → clear both sensors (if defined).
                    if (block.EnterSensorId != 0)
                    {
                        sensorEvents.Add((block.EnterSensorId, false));
                    }

                    if (block.ExitSensorId != 0 && block.ExitSensorId != block.EnterSensorId)
                    {
                        sensorEvents.Add((block.ExitSensorId, false));
                    }

                    // Determine the next block, taking into account switch states.
                    int? nextBlockId = GetNextBlockIdConsideringSwitches(loco, block);

                    if (nextBlockId == null || !_blocks.TryGetValue(nextBlockId.Value, out var nextBlock))
                    {
                        // No valid next block → train stops at the end of this block.
                        loco.SpeedSteps = 0;
                        continue;
                    }

                    // Enter the next block. Place the loco at the start or end depending on direction.
                    loco.BlockId = nextBlock.BlockId;
                    loco.PositionMm = loco.Direction > 0 ? 0 : nextBlock.LengthMm;
                    loco.ExitZoneReached = false;

                    // Entering the new block → first only the enter sensor becomes occupied.
                    if (nextBlock.EnterSensorId != 0)
                    {
                        sensorEvents.Add((nextBlock.EnterSensorId, true));
                    }

                    // Exit sensor of the new block will be set once the loco passes the midpoint.
                }
            }

            // Outside the lock: send all sensor events to the feedback sink (fire-and-forget).
            if (sensorEvents.Count > 0)
            {
                foreach (var (sensorId, occupied) in sensorEvents)
                {
                    // We do not await here to keep the simulation loop non-blocking.
                    _ = _feedbackSink.OnSensorChangedAsync(sensorId, occupied);
                }
            }
        }

        private int? GetNextBlockIdConsideringSwitches(LocoSimState loco, BlockDef block)
        {
            // Default volgens de statische definitie
            int? defaultNext = loco.Direction > 0
                ? block.NextBlockForward
                : block.NextBlockReverse;

            int currentBlockId = block.BlockId;

            // Decoder-adressen van de twee wissels (afgeleid uit je ECoS-log):
            //  - wissel 1: addr=1 (tussen blok 3 en blok 4/5)
            //  - wissel 2: addr=2 (tussen blok 4/5 en blok 1)
            const int LadderSwitch1Decoder = 1;
            const int LadderSwitch2Decoder = 2;

            // Standen:
            //  0 = recht, 1 = afbuigend
            int sw1Pos = GetSwitchPosition(LadderSwitch1Decoder);
            int sw2Pos = GetSwitchPosition(LadderSwitch2Decoder);

            if (loco.Direction > 0)
            {
                // Rijrichting "vooruit" (zoals 1→2→3→(4/5)→1)

                if (currentBlockId == 3)
                {
                    // Vanuit blok 3 naar blok 4 of 5:
                    // wissel 1 bepaalt:
                    //   0 = recht  -> blok 4
                    //   1 = afbuig -> blok 5
                    if (sw1Pos == 0) return 4;
                    if (sw1Pos == 1) return 5;

                    return defaultNext;
                }

                if (currentBlockId == 4)
                {
                    // Vanuit blok 4 naar blok 1:
                    // wissel 2 moet "recht" staan (0 = spoor 4 ↔ blok 1)
                    if (sw2Pos == 0) return 1;

                    // Wissel verkeerd -> geen geldig vervolgblok, trein stopt aan eind blok 4
                    return null;
                }

                if (currentBlockId == 5)
                {
                    // Vanuit blok 5 naar blok 1:
                    // wissel 2 moet "afbuigend" staan (1 = spoor 5 ↔ blok 1)
                    if (sw2Pos == 1) return 1;

                    // Wissel verkeerd -> geen geldig vervolgblok, trein stopt aan eind blok 5
                    return null;
                }
            }
            else // loco.Direction < 0
            {
                // Rijrichting achteruit (zoals 1→(4/5)→3→2→1)

                if (currentBlockId == 1)
                {
                    // Vanuit blok 1 de wisselstraat in:
                    // wissel 2 bepaalt:
                    //   0 = spoor 4
                    //   1 = spoor 5
                    if (sw2Pos == 0) return 4;
                    if (sw2Pos == 1) return 5;

                    return defaultNext;
                }

                if (currentBlockId == 4)
                {
                    // Vanuit blok 4 terug naar blok 3:
                    // wissel 1 moet "recht" staan (0 = spoor 4 ↔ blok 3)
                    if (sw1Pos == 0) return 3;

                    // Wissel verkeerd -> geen geldig vervolgblok
                    return null;
                }

                if (currentBlockId == 5)
                {
                    // Vanuit blok 5 terug naar blok 3:
                    // wissel 1 moet "afbuigend" staan (1 = spoor 5 ↔ blok 3)
                    if (sw1Pos == 1) return 3;

                    // Wissel verkeerd -> geen geldig vervolgblok
                    return null;
                }
            }

            // Voor alle andere blokken, of als er geen speciale logica geldt:
            return defaultNext;
        }

        /// <summary>
        /// Voor nu: 4 blokken in een rondje, 1 sensor per blok (1..4).
        /// </summary>
        /// <summary>
        /// Ovaal met 3 hoofd-blokken (1-2-3) en een wisselstraat
        /// naar blok 4 en 5. Alle blokken hebben 2 bezetmelders.
        /// Sensor IDs: 1..10.
        /// </summary>
        private void InitOvalWithLadder()
        {
            _blocks.Clear();

            // Blok 1: tussen blok 3 en 2
            _blocks[1] = new BlockDef(
                BlockId: 1,
                EnterSensorId: 1,
                ExitSensorId: 2,
                LengthMm: 1000,
                NextBlockForward: 2,  // 1→2
                NextBlockReverse: 3   // 1→3
            );

            // Blok 2: tussen blok 1 en 3
            _blocks[2] = new BlockDef(
                BlockId: 2,
                EnterSensorId: 3,
                ExitSensorId: 4,
                LengthMm: 1000,
                NextBlockForward: 3,  // 2→3
                NextBlockReverse: 1   // 2→1
            );

            // Blok 3: tussen blok 2 en 1 / wisselstraat
            _blocks[3] = new BlockDef(
                BlockId: 3,
                EnterSensorId: 5,
                ExitSensorId: 6,
                LengthMm: 1000,
                NextBlockForward: 1,  // default hoofdbaan 3→1
                NextBlockReverse: 2   // 3→2
            );

            // Blok 4: eerste spoor van wisselstraat
            _blocks[4] = new BlockDef(
                BlockId: 4,
                EnterSensorId: 7,
                ExitSensorId: 8,
                LengthMm: 800,
                NextBlockForward: 1,  // 4→1
                NextBlockReverse: 3   // 4→3
            );

            // Blok 5: tweede spoor van wisselstraat
            _blocks[5] = new BlockDef(
                BlockId: 5,
                EnterSensorId: 9,
                ExitSensorId: 10,
                LengthMm: 800,
                NextBlockForward: 1,  // 5→1
                NextBlockReverse: 3   // 5→3
            );
        }


        // ========== Hulp-types ==========

        // ========== Hulp-types ==========

        private sealed class LocoSimState
        {
            public int DecoderAddress { get; set; }
            public int BlockId { get; set; }
            public double PositionMm { get; set; }
            public int Direction { get; set; }
            public int SpeedSteps { get; set; }

            /// <summary>
            /// Tracks whether the locomotive has already passed the "exit zone"
            /// (midpoint of the current block), so we only trigger the exit sensor once.
            /// </summary>
            public bool ExitZoneReached { get; set; }
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
