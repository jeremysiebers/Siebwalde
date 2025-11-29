using System.Runtime.CompilerServices;
using System.Threading.Channels;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Core.Host
{
    /// <summary>
    /// Fake transport that simulates the PIC32 Ethernet target.
    /// It receives raw frames from TrackCommClientAsync and pushes
    /// simulated reply frames back through a channel.
    /// </summary>
    public sealed class FakeTrackTransport : ITrackTransport
    {
        private readonly Channel<byte[]> _incoming =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private bool _isOpen;

        // --- Firmware download simulation ---
        // We simulate that the full PROGMEMSIZE bytes are written.
        // The number of FW data packets depends on the effective
        // payload size sent by SendNextFwDataPacket.
        private int _fwBytesRemaining;
        private bool _fwDownloadActive;

        // --- Fault injection flags ---
        // You can toggle these from the host to exercise
        // error paths in RecoverSlavesStep / FlashFwTrackamplifiersStep.
        public bool ForceGetBootloaderVersionError { get; set; }
        public bool ForceEraseFlashError { get; set; }
        public bool ForceChecksumError { get; set; }

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            _isOpen = true;
            Console.WriteLine("[FAKE] Transport opened.");
            await Task.CompletedTask;
        }

        public async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            _isOpen = false;
            _incoming.Writer.TryComplete();
            Console.WriteLine("[FAKE] Transport closed.");
            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
        }

        public async Task SendAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken = default)
        {
            if (!_isOpen)
                throw new InvalidOperationException("FakeTrackTransport is not opened.");

            // Copy the transmitted frame (HEADER + command + optional payload)
            var frame = new byte[count];
            Buffer.BlockCopy(buffer, offset, frame, 0, count);

            //Console.WriteLine($"[FAKE] TX ({count} bytes): {BitConverter.ToString(frame)}");

            if (count < 2 || frame[0] != Enums.HEADER)
            {
                // Unknown frame (no header) → ignore, same behavior as UDP transport.
                Console.WriteLine("[FAKE] TX without valid HEADER, ignored.");
                return;
            }

            byte clientCommand = frame[1];

            byte taskId;
            byte taskCommand;
            byte taskState;
            byte taskMsg = 0x00;   // default: no error

            bool sendSlaveInfoFrames = false;

            // -----------------------------------------------------------------
            // 1. Controller connect
            // -----------------------------------------------------------------
            if (clientCommand == EnumClientCommands.CLIENT_CONNECTION_REQUEST)
            {
                taskId = TaskId.CONTROLLER;
                taskCommand = TaskStates.CONNECTED;
                taskState = TaskStates.DONE;
            }
            // -----------------------------------------------------------------
            // 2. MBUS state machine (initialization pipeline)
            // -----------------------------------------------------------------
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_RESET)
            {
                taskId = TaskId.MBUS;
                taskCommand = EnumMbusStatus.MBUS_STATE_RESET;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_SLAVES_ON)
            {
                taskId = TaskId.MBUS;
                taskCommand = EnumMbusStatus.MBUS_STATE_SLAVES_ON;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_SLAVES_BOOT_WAIT)
            {
                taskId = TaskId.MBUS;
                taskCommand = EnumMbusStatus.MBUS_STATE_SLAVES_BOOT_WAIT;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH)
            {
                // This starts the FWHANDLER task.
                taskId = TaskId.FWHANDLER;
                taskCommand = TrackCommand.FWHANDLERINIT;
                taskState = TaskStates.CONNECTED;
            }
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT)
            {
                taskId = TaskId.MBUS;
                taskCommand = EnumMbusStatus.MBUS_STATE_SLAVE_INIT;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE)
            {
                taskId = TaskId.MBUS;
                taskCommand = EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD)
            {
                taskId = TaskId.MBUS;
                taskCommand = EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT)
            {
                // DetectSlavesStep waits for this MBUS status.
                taskId = TaskId.MBUS;
                taskCommand = EnumMbusStatus.MBUS_STATE_SLAVE_DETECT;
                taskState = TaskStates.DONE;

                // Additionally, we simulate slave data frames for all slaves.
                sendSlaveInfoFrames = true;
            }
            // -----------------------------------------------------------------
            // 3. FWHANDLER / bootloader commands
            // -----------------------------------------------------------------
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_GET_BOOTLOADER_VERSION)
            {
                taskId = TrackCommand.GET_BOOTLOADER_VERSION;

                if (ForceGetBootloaderVersionError)
                {
                    taskCommand = TrackCommand.GET_BOOTLOADER_VERSION_NOK;
                    taskState = TaskStates.ERROR;
                }
                else
                {
                    taskCommand = TrackCommand.GET_BOOTLOADER_VERSION_OK;
                    taskState = TaskStates.DONE;
                }
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_ERASE_FLASH)
            {
                taskId = TrackCommand.ERASE_FLASH;

                if (ForceEraseFlashError)
                {
                    taskCommand = TrackCommand.ERASE_FLASH_RETURNED_NOK;
                    taskState = TaskStates.ERROR;
                }
                else
                {
                    taskCommand = TrackCommand.ERASE_FLASH_RETURNED_OK;
                    taskState = TaskStates.DONE;
                }
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE)
            {
                // Start a new firmware download session.
                StartNewFirmwareDownloadSession();

                taskId = TrackCommand.FWFILEDOWNLOAD;
                taskCommand = TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE)
            {
                // SendNextFwDataPacket uses this command repeatedly.
                taskId = TrackCommand.FWFILEDOWNLOAD;

                if (!_fwDownloadActive)
                {
                    // No active session: just signal "done".
                    taskCommand = TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE;
                    taskState = TaskStates.DONE;
                }
                else
                {
                    // All bytes after HEADER + command are treated as payload.
                    int payloadBytes = Math.Max(0, count - 2);

                    if (payloadBytes == 0)
                    {
                        // Fallback: approximate using HEXROWWIDTH * JUMPSIZE.
                        payloadBytes = Enums.HEXROWWIDTH * Enums.JUMPSIZE;
                    }

                    if (_fwBytesRemaining > payloadBytes)
                    {
                        _fwBytesRemaining -= payloadBytes;
                        taskCommand = TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY;
                        taskState = TaskStates.DONE;
                    }
                    else
                    {
                        _fwBytesRemaining = 0;
                        _fwDownloadActive = false;
                        taskCommand = TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE;
                        taskState = TaskStates.DONE;
                    }
                }
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_CHECK_CHECKSUM)
            {
                taskId = TrackCommand.FWFILEDOWNLOAD;
                taskCommand = TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM;
                taskState = TaskStates.DONE;
                taskMsg = ForceChecksumError
                    ? TaskMessages.RECEIVED_CHECKSUM_NOK
                    : TaskMessages.RECEIVED_CHECKSUM_OK;
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD)
            {
                taskId = TrackCommand.FWCONFIGWORDDOWNLOAD;
                taskCommand = TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE)
            {
                taskId = TrackCommand.FWCONFIGWORDDOWNLOAD;
                taskCommand = TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES)
            {
                // FlashFwTrackamplifiersStep waits for this to complete.
                taskId = TaskId.FWHANDLER;
                taskCommand = TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_WRITE_FLASH)
            {
                taskId = TrackCommand.WRITE_FLASH;
                taskCommand = TrackCommand.WRITE_FLASH_RETURNED_OK;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_WRITE_CONFIG)
            {
                taskId = TrackCommand.WRITE_CONFIG;
                taskCommand = TrackCommand.WRITE_CONFIG_RETURNED_OK;
                taskState = TaskStates.DONE;
            }
            else if (clientCommand == TrackCommand.EXEC_FW_STATE_SLAVE_RESET)
            {
                taskId = TrackCommand.RESET_SLAVE;
                taskCommand = TrackCommand.RESET_SLAVE_OK;
                taskState = TaskStates.DONE;
            }
            else
            {
                // Generic "OK" fallback so you can see unhandled commands in the logs.
                taskId = TaskId.CONTROLLER;
                taskCommand = clientCommand;
                taskState = TaskStates.DONE;
            }

            // 1) Controller / bootloader status message
            var reply = new byte[]
            {
                Enums.HEADER,
                taskId,
                taskCommand,
                taskState,
                taskMsg
            };

            Console.WriteLine($"[FAKE] RX injected (control): {BitConverter.ToString(reply)}");
            await _incoming.Writer.WriteAsync(reply, cancellationToken).ConfigureAwait(false);

            // 2) Optional: SLAVEINFO frames for DetectSlavesStep.
            if (sendSlaveInfoFrames)
            {
                foreach (byte slaveId in GetSimulatedSlaveIds())
                {
                    var slaveFrame = CreateSlaveInfoFrame(slaveId, slaveDetected: 1);
                    Console.WriteLine($"[FAKE] RX injected (slave {slaveId}): {BitConverter.ToString(slaveFrame)}");
                    await _incoming.Writer.WriteAsync(slaveFrame, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async IAsyncEnumerable<byte[]> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await _incoming.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_incoming.Reader.TryRead(out var frame))
                {
                    yield return frame;
                }
            }
        }

        // --------------------------------------------------------------------
        // Helper methods
        // --------------------------------------------------------------------

        private void StartNewFirmwareDownloadSession()
        {
            _fwDownloadActive = true;
            _fwBytesRemaining = Enums.PROGMEMSIZE; // size of flash in bytes
        }

        private static IEnumerable<byte> GetSimulatedSlaveIds()
        {
            // 50 "normal" slaves on IDs 1..50
            for (byte id = 1; id <= 50; id++)
                yield return id;

            // 5 backpanel slaves on IDs 51..55
            for (byte id = 51; id <= 55; id++)
                yield return id;
        }

        /// <summary>
        /// Builds a SLAVEINFO frame identical to what EthernetTargetDataSimulator
        /// and TrackIOHandle used to send:
        ///
        /// 0  : HEADER
        /// 1  : SLAVEINFO
        /// 2  : mbHeader (HEADER again)
        /// 3  : slaveNumber
        /// 4  : slaveDetected (1 = present)
        /// 5  : padding
        /// 6-29  : 12 x HoldingReg (UInt16, little-endian)
        /// 30-31 : MbReceiveCounter (UInt16, little-endian)
        /// 32-33 : MbSentCounter (UInt16, little-endian)
        /// 34-37 : MbCommError (UInt32, little-endian)
        /// 38    : MbExceptionCode (byte)
        /// 39    : SpiCommErrorCounter (byte)
        /// 40    : FOOTER
        /// </summary>
        private static byte[] CreateSlaveInfoFrame(byte slaveNumber, byte slaveDetected)
        {
            var data = new byte[41];

            data[0] = Enums.HEADER;
            data[1] = Enums.SLAVEINFO;
            data[2] = Enums.HEADER;
            data[3] = slaveNumber;
            data[4] = slaveDetected;
            data[5] = 0; // padding

            ushort holding = 0;
            ushort mbReceiveCounter = 0;
            ushort mbSentCounter = 0;
            uint mbCommError = 0;
            byte mbExceptionCode = 0;
            byte spiErrors = 0;

            ushort j = 0;
            for (ushort i = 6; i < 30; i += 2)
            {
                data[i] = (byte)(holding & 0x00FF);
                data[i + 1] = (byte)((holding & 0xFF00) >> 8);
                j++;
            }

            data[30] = (byte)(mbReceiveCounter & 0x00FF);
            data[31] = (byte)((mbReceiveCounter & 0xFF00) >> 8);

            data[32] = (byte)(mbSentCounter & 0x00FF);
            data[33] = (byte)((mbSentCounter & 0xFF00) >> 8);

            data[34] = (byte)(mbCommError & 0x000000FF);
            data[35] = (byte)((mbCommError & 0x0000FF00) >> 8);
            data[36] = (byte)((mbCommError & 0x00FF0000) >> 16);
            data[37] = (byte)((mbCommError & 0xFF000000) >> 24);

            data[38] = mbExceptionCode;
            data[39] = spiErrors;
            data[40] = Enums.FOOTER;

            return data;
        }
    }
}
