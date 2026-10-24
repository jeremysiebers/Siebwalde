using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>RAM parameters owned by a physical position, not by a device or operational group.</summary>
    public sealed record AmplifierConfiguration(byte Acceleration = 2, byte Deceleration = 2,
        byte FastStop = 8, ushort WatchdogMs = 250, ushort Tag = 1);

    public sealed class AmplifierConfigurationDownloader
    {
        private readonly ITrackCommClient _comm;
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
        public AmplifierConfigurationDownloader(ITrackCommClient comm) => _comm = comm;

        public static SendMessage Exchange(ushort slave, byte register, ushort value, bool read = false)
            => new(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH,
                new byte[] { checked((byte)slave), read ? (byte)0x55 : (byte)0xAA, 1, register,
                    (byte)value, (byte)(value >> 8) });

        public async Task<bool> DownloadAsync(TrackAmplifierItem item, AmplifierConfiguration config, CancellationToken ct)
        {
            item.ConfigurationVerified = false;
            if (!AmplifierPhase1Safety.Compatible(item))
            {
                item.Phase1Failure = AmplifierPhase1Safety.IncompatibilityReason(item)
                    ?? AmplifierPhase1Safety.IncompatibleFirmware;
                return false;
            }
            if ((item.HoldingReg[7] & TrackAmplifierRegisters.TwoPhaseConfigCapability) == 0)
            {
                item.Phase1Failure = "two-phase configuration unsupported";
                return false;
            }
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(Timeout);
            try
            {
                var values = new ushort[] { (ushort)(config.Acceleration | config.Deceleration << 8), config.FastStop,
                    config.WatchdogMs, config.Tag };
                for (byte i = 0; i < values.Length; i++)
                {
                    byte address = (byte)(3 + i);
                    await WriteAndVerifyAsync(item, address, values[i], deadline.Token).ConfigureAwait(false);
                }
                // Explicit 0 then 1: never depend on a concurrent lease renewal to re-arm APPLY.
                await WriteAndVerifyAsync(item, 1, 0, deadline.Token).ConfigureAwait(false);
                long sequence = item.ProtocolReadbackSequence;
                await _comm.SendAsync(Exchange(item.SlaveNumber, 1, TrackAmplifierRegisters.ApplyConfigBit), deadline.Token).ConfigureAwait(false);
                await _comm.SendAsync(Exchange(item.SlaveNumber, 8, 0, true), deadline.Token).ConfigureAwait(false);
                await _comm.SendAsync(Exchange(item.SlaveNumber, 15, 0, true), deadline.Token).ConfigureAwait(false);
                await WaitAsync(() => item.ProtocolReadbackSequence > sequence && item.HoldingReg[15] == 1 &&
                    (item.HoldingReg[8] & 0x0600) == 0x0600 && values.SequenceEqual(item.HoldingReg.Skip(3).Take(4)), deadline.Token).ConfigureAwait(false);
                if (!AmplifierPhase1Safety.Compatible(item)) throw new InvalidOperationException("compatibility changed during configuration");
                item.ConfigurationVerified = true;
                item.Phase1Failure = null;
                return true;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            { item.Phase1Failure = $"configuration download/verification timeout; write-result=0x{item.HoldingReg[15]:X4}"; return false; }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { item.Phase1Failure = $"configuration download failed: {ex.Message}"; return false; }
        }

        private async Task WriteAndVerifyAsync(TrackAmplifierItem item, byte address, ushort value, CancellationToken ct)
        {
            long sequence = item.ProtocolReadbackSequence;
            await _comm.SendAsync(Exchange(item.SlaveNumber, address, value), ct).ConfigureAwait(false);
            await _comm.SendAsync(Exchange(item.SlaveNumber, address, 0, true), ct).ConfigureAwait(false);
            await _comm.SendAsync(Exchange(item.SlaveNumber, 15, 0, true), ct).ConfigureAwait(false);
            await WaitAsync(() => item.ProtocolReadbackSequence > sequence && item.HoldingReg[15] == address &&
                (address == 1 || item.HoldingReg[address] == value), ct).ConfigureAwait(false);
        }

        private static async Task WaitAsync(Func<bool> predicate, CancellationToken ct)
        { while (!predicate()) await Task.Delay(10, ct).ConfigureAwait(false); }
    }
}
