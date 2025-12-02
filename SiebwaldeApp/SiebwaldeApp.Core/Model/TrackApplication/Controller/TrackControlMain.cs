using System;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Main high-level track controller.
    /// 
    /// The legacy sequencer logic is removed. Initialization is handled by
    /// TrackAmplifierInitializationServiceAsync.
    /// 
    /// This class is responsible for:
    /// - Holding a reference to the track application variables
    /// - Periodically (10 Hz) checking for pending amplifier writes
    /// - Sending EXEC_MBUS_SLAVE_DATA_EXCH commands to the master when data changed
    /// </summary>
    public class TrackControlMain
    {
        #region Private fields

        private readonly string _loggerInstance;
        private readonly ITrackCommClient _trackCommClient;
        private readonly TrackApplicationVariables mTrackApplicationVariables;

        private readonly System.Timers.Timer _runtimeTimer;
        private readonly object _syncRoot = new();

        private bool _isRunning;
        private bool _tickInProgress;
        private CancellationToken _cancellationToken;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new TrackControlMain instance.
        /// </summary>
        /// <param name="loggerInstance">Logger instance name to use for IoC.Logger.</param>
        /// <param name="trackCommClient">Low-level track communication client.</param>
        /// <param name="variables">Shared track application variables.</param>
        public TrackControlMain(
            string loggerInstance,
            ITrackCommClient trackCommClient,
            TrackApplicationVariables variables)
        {
            _loggerInstance = string.IsNullOrWhiteSpace(loggerInstance) ? "Track" : loggerInstance;
            _trackCommClient = trackCommClient ?? throw new ArgumentNullException(nameof(trackCommClient));
            mTrackApplicationVariables = variables ?? throw new ArgumentNullException(nameof(variables));

            // Runtime timer at 10 Hz (100 ms interval).
            _runtimeTimer = new System.Timers.Timer(100)
            {
                AutoReset = true
            };
            _runtimeTimer.Elapsed += RuntimeTimerElapsed;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts the runtime loop that processes pending amplifier writes.
        /// This is intended to be called AFTER the initialization sequence completed.
        /// </summary>
        public void StartRuntime(CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                if (_isRunning)
                    return;

                _cancellationToken = cancellationToken;
                _isRunning = true;
                _runtimeTimer.Start();

                IoC.Logger.Log("TrackControlMain runtime loop started.", _loggerInstance);
            }
        }

        /// <summary>
        /// Stops the runtime loop.
        /// </summary>
        public void StopRuntime()
        {
            lock (_syncRoot)
            {
                if (!_isRunning)
                    return;

                _runtimeTimer.Stop();
                _isRunning = false;

                IoC.Logger.Log("TrackControlMain runtime loop stopped.", _loggerInstance);
            }
        }

        #endregion

        #region Timer processing

        /// <summary>
        /// Executed at 10 Hz by the timer.
        /// Sends pending writes to amplifiers if needed.
        /// </summary>
        private async void RuntimeTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isRunning)
                return;

            // Simple re-entrancy guard; timer can fire on a thread pool thread
            // while a previous tick is still running.
            if (_tickInProgress)
                return;

            try
            {
                _tickInProgress = true;

                await ProcessPendingAmplifierWritesAsync(_cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal during shutdown/cancellation; no log required.
            }
            catch (Exception ex)
            {
                IoC.Logger.Log($"TrackControlMain runtime tick error: {ex}", _loggerInstance);
            }
            finally
            {
                _tickInProgress = false;
            }
        }

        /// <summary>
        /// Scans the TrackApplicationVariables.PendingWrites dictionary and sends
        /// EXEC_MBUS_SLAVE_DATA_EXCH commands for each pending HoldingReg0 update.
        /// </summary>
        private async Task ProcessPendingAmplifierWritesAsync(CancellationToken cancellationToken)
        {
            var pending = mTrackApplicationVariables.PendingWrites;
            if (pending == null || pending.Count == 0)
                return;

            // Take a snapshot to avoid issues if the dictionary is modified
            // while we are iterating.
            var items = pending.Values.ToArray();

            foreach (var writeData in items)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (writeData == null)
                    continue;

                if (writeData.TryConsumeHr0(out ushort hr0Value))
                {
                    // We only write HoldingReg0 (index 0) for now
                    var message = BuildSlaveDataExchWriteMessage(
                        writeData.SlaveNumber,
                        startRegister: 0,
                        hr0Value);

                    await _trackCommClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

                    IoC.Logger.Log(
                        $"[WRITE] EXEC_MBUS_SLAVE_DATA_EXCH: slave={writeData.SlaveNumber}, HR0=0x{hr0Value:X4}",
                        _loggerInstance);
                }

                // Future extension: other holding registers (HR1, HR2, ...) could be handled here.
            }
        }

        #endregion

        #region Helper to build EXEC_MBUS_SLAVE_DATA_EXCH message

        /// <summary>
        /// Builds a SendMessage for EXEC_MBUS_SLAVE_DATA_EXCH to write 1 or 2
        /// consecutive holding registers to a given slave.
        /// 
        /// Format:
        /// data[0] = SlaveAddress (1..50)
        /// data[1] = Direction (read = 0x55, write = 0xAA)
        /// data[2] = No of registers (1 or 2)
        /// data[3] = Start register index
        /// data[4] = RegisterData0 (low byte)
        /// data[5] = RegisterData0 (high byte)
        /// data[6] = RegisterData1 (low byte, optional)
        /// data[7] = RegisterData1 (high byte, optional)
        /// </summary>
        private static SendMessage BuildSlaveDataExchWriteMessage(
            ushort slaveNumber,
            byte startRegister,
            params ushort[] registers)
        {
            if (registers == null || registers.Length == 0 || registers.Length > 2)
                throw new ArgumentException("Only 1 or 2 registers are supported.", nameof(registers));

            byte registerCount = (byte)registers.Length;

            int dataLength = 4 + registerCount * 2;
            var data = new byte[dataLength];

            data[0] = (byte)slaveNumber;
            data[1] = 0xAA; // write direction
            data[2] = registerCount;
            data[3] = startRegister;

            int offset = 4;
            for (int i = 0; i < registerCount; i++)
            {
                ushort value = registers[i];
                data[offset++] = (byte)(value & 0xFF);        // low byte
                data[offset++] = (byte)((value >> 8) & 0xFF); // high byte
            }

            return new SendMessage(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, data);
        }

        #endregion
    }
}
