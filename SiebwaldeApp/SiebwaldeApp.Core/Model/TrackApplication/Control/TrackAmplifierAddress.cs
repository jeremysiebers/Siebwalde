namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Authoritative physical-device classification for ModBus slave addresses on the track bus.
    ///
    /// This is the single definition of "what is a track amplifier". It must be used by every
    /// path that applies track-amplifier PWM semantics (HoldingReg0 bits 0..9), so that those
    /// semantics can never reach a device of another physical class.
    ///
    /// Verified ranges (repository source):
    /// - Track amplifiers occupy <b>1..50</b>:
    ///   <c>TrackApplicationVariables.MaxAmplifiers</c>, the <c>TrackAmplifierWriteData</c> doc
    ///   ("1..50 for track amplifiers"), <c>TrackAmplifierPageViewModel</c> ("1..50 amplifiers"),
    ///   <c>FlashFwTrackamplifiersStep</c> (<c>SlaveNumber &lt; 51</c>),
    ///   <c>EthernetTargetDataSimulator</c> (<c>SlaveNumber &lt; 51</c>), and
    ///   <c>TrackControlMain</c> ("data[0] = SlaveAddress (1..50)").
    /// - Backplane/configuration modules occupy <b>51..55</b>:
    ///   <c>TrackAmplifierPageViewModel</c> ("51..55 backplane modules") and
    ///   <c>TrackBackplane2.X/main_proto_backplane.c</c> <c>Get_ID()</c>, which derives
    ///   <c>MODBUS_ADDRESS = 50 + ID-pin</c>.
    ///
    /// The backplane modules use HoldingReg0 as a configuration/enable word
    /// (<c>ActValue</c> in <c>TrackBackplane2.X/main_proto_backplane.c</c> selects which
    /// amplifier IDs are enabled). Writing the track-amplifier neutral value 399 (0x018F) there
    /// would alter configuration bits, so it must never happen.
    /// </summary>
    public static class TrackAmplifierAddress
    {
        /// <summary>Lowest track-amplifier slave address.</summary>
        public const int MinTrackAmplifier = 1;

        /// <summary>Highest track-amplifier slave address.</summary>
        public const int MaxTrackAmplifier = 50;

        /// <summary>Lowest backplane/configuration slave address.</summary>
        public const int MinBackplaneSlave = 51;

        /// <summary>Highest backplane/configuration slave address.</summary>
        public const int MaxBackplaneSlave = 55;

        /// <summary>
        /// True when the address is a legitimate physical track amplifier. Track-amplifier PWM
        /// semantics are legal only for such an address.
        /// </summary>
        public static bool IsTrackAmplifierAddress(int address)
            => address >= MinTrackAmplifier && address <= MaxTrackAmplifier;

        /// <summary>
        /// True when the address is a backplane/configuration module. Such a device must never
        /// receive a track-amplifier PWM/neutral write.
        /// </summary>
        public static bool IsBackplaneConfigurationSlave(int address)
            => address >= MinBackplaneSlave && address <= MaxBackplaneSlave;

        /// <summary>True when the address belongs to a known physical slave class.</summary>
        public static bool IsKnownSlaveAddress(int address)
            => IsTrackAmplifierAddress(address) || IsBackplaneConfigurationSlave(address);
    }
}
