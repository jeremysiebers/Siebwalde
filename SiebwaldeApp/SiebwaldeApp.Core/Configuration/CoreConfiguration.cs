using SiebwaldeApp.Core.Properties;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Single source of truth for core configuration values.
    /// Values are read from <see cref="CoreSettings"/> so they can be edited in configuration
    /// instead of being hard-coded in the startup code.
    /// </summary>
    public static class CoreConfiguration
    {
        /// <summary>IP address of the track controller (TrackController5).</summary>
        public static string TrackControllerIpAddress => CoreSettings.Default.TrckIpAddress;

        /// <summary>UDP port the track controller listens on.</summary>
        public static int TrackControllerSendingPort => CoreSettings.Default.TrckSendingPort;

        /// <summary>Local UDP port used to receive track controller replies.</summary>
        public static int TrackControllerReceivingPort => CoreSettings.Default.TrckReceivingPort;

        /// <summary>Path of the track amplifier firmware hex file.</summary>
        public static string TrackAmplifierFirmwarePath => CoreSettings.Default.TrackAmplifierFwPath;

        /// <summary>Directory where log files are written.</summary>
        public static string LogDirectory => CoreSettings.Default.LogDirectory;

        /// <summary>UDP port used to send to the Fiddle Yard controller.</summary>
        public static int FiddleYardSendingPort => CoreSettings.Default.FYSendingport;

        /// <summary>UDP port used to receive from the Fiddle Yard controller.</summary>
        public static int FiddleYardReceivingPort => CoreSettings.Default.FYReceivingport;

        /// <summary>Raw topology configuration text (amplifier sections and routes).</summary>
        public static string BlockTopologyConfig => CoreSettings.Default.BlockTopologyConfig;

        /// <summary>Raw Koploper block mapping text (block -> bezetmelders -> amplifier sections).</summary>
        public static string KoploperBlockMapConfig => CoreSettings.Default.KoploperBlockMapConfig;

        /// <summary>Parses <see cref="BlockTopologyConfig"/> into a topology.</summary>
        public static BlockTopology BuildBlockTopology() => BlockTopology.Parse(BlockTopologyConfig);

        /// <summary>Parses <see cref="KoploperBlockMapConfig"/> into a Koploper block map.</summary>
        public static KoploperBlockMap BuildKoploperBlockMap() => KoploperBlockMap.Parse(KoploperBlockMapConfig);
    }
}
