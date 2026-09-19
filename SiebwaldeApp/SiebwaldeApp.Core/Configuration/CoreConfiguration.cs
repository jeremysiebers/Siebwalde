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

        /// <summary>
        /// Raw topology configuration text (amplifier sections and routes). Falls back to the
        /// setting's declared default when the persisted user value is null, empty or
        /// whitespace, so a legacy user.config cannot silently disable the mapping.
        /// </summary>
        public static string BlockTopologyConfig => ResolveSettingOrDefault(
            CoreSettings.Default.BlockTopologyConfig,
            nameof(CoreSettings.BlockTopologyConfig));

        /// <summary>Raw Koploper block mapping text (block -> bezetmelders -> amplifier sections).</summary>
        public static string KoploperBlockMapConfig => ResolveSettingOrDefault(
            CoreSettings.Default.KoploperBlockMapConfig,
            nameof(CoreSettings.KoploperBlockMapConfig));

        /// <summary>Raw switch mapping text (ECoS switch address -> physical switch output).</summary>
        public static string SwitchMapConfig => ResolveSettingOrDefault(
            CoreSettings.Default.SwitchMapConfig,
            nameof(CoreSettings.SwitchMapConfig));

        /// <summary>Parses <see cref="BlockTopologyConfig"/> into a topology.</summary>
        public static BlockTopology BuildBlockTopology() => BlockTopology.Parse(BlockTopologyConfig);

        /// <summary>Parses <see cref="KoploperBlockMapConfig"/> into a Koploper block map.</summary>
        public static KoploperBlockMap BuildKoploperBlockMap() => KoploperBlockMap.Parse(KoploperBlockMapConfig);

        /// <summary>
        /// Parses <see cref="SwitchMapConfig"/> into a switch mapping. Invalid entries are
        /// recorded in <see cref="SwitchMapping.Errors"/> and logged, so a malformed
        /// configuration never silently becomes a plausible but wrong physical mapping.
        /// </summary>
        public static SwitchMapping BuildSwitchMap()
        {
            var mapping = SwitchMapping.Parse(SwitchMapConfig);

            foreach (var error in mapping.Errors)
            {
                IoC.Logger.Log($"Switch mapping problem: {error}", "CoreConfiguration");
            }

            return mapping;
        }

        /// <summary>
        /// Returns the effective setting value, or the declared default when the persisted user
        /// value is missing or blank.
        ///
        /// A non-empty value is always used as-is, even when it is malformed: configuration
        /// errors must stay diagnosable instead of being masked by the default.
        /// </summary>
        /// <param name="effectiveValue">The persisted user value, or the default when none was stored.</param>
        /// <param name="settingName">Name of the setting, used to read the declared default.</param>
        public static string ResolveSettingOrDefault(string? effectiveValue, string settingName)
        {
            if (!string.IsNullOrWhiteSpace(effectiveValue))
            {
                return effectiveValue;
            }

            // Read the default from the settings metadata instead of duplicating the mapping
            // string in code, so the Designer remains the single source of truth.
            var declaredDefault = CoreSettings.Default.Properties[settingName]?.DefaultValue as string
                                  ?? string.Empty;

            IoC.Logger.Log(
                $"Setting '{settingName}' is empty; using the configured default.",
                "CoreConfiguration");

            return declaredDefault;
        }
    }
}
