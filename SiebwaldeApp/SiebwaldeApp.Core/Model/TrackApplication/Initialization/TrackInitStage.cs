namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The seven controlled-initialization stages of the Ovaaltje2 startup state machine
    /// (docs/ovaaltje2-init-state-machine-design.md §3). Stage order is fixed; a stage may only
    /// advance when its preconditions are satisfied, and Stage 6 (track power) is always
    /// operator-gated so outputs are never silently energized.
    /// </summary>
    public enum TrackInitStage
    {
        /// <summary>Connect to the Ethernet target, query the controller version and observe the global-stop state.</summary>
        ControllerConnection = 1,

        /// <summary>Reset/detect/recover slaves and flash firmware (flash only under an authorized state).</summary>
        FirmwareAddress = 2,

        /// <summary>Each amplifier is addressed and in STANDBY (H-bridge inhibited, config download permitted).</summary>
        AmplifierStandby = 3,

        /// <summary>Config download + APPLY + verify INIT_DONE/CONFIG_OK and safety preconditions per amplifier.</summary>
        ConfigureValidate = 4,

        /// <summary>All required amplifiers are READY (H-bridge de-energized, no movement).</summary>
        GlobalReady = 5,

        /// <summary>Explicit, gated READY -> TRACK_POWER_ON transition: neutral track-power energize.</summary>
        AmpPowerEnable = 6,

        /// <summary>Normal operation: the runtime loop with the existing occupancy + movement-authority logic.</summary>
        NormalOperation = 7
    }

    /// <summary>Display names and ordering helpers for <see cref="TrackInitStage"/>.</summary>
    public static class TrackInitStageInfo
    {
        public static string DisplayName(TrackInitStage stage) => stage switch
        {
            TrackInitStage.ControllerConnection => "Controller connection",
            TrackInitStage.FirmwareAddress => "Firmware / address verification",
            TrackInitStage.AmplifierStandby => "Amplifier STANDBY",
            TrackInitStage.ConfigureValidate => "Configure and validate",
            TrackInitStage.GlobalReady => "Global ready",
            TrackInitStage.AmpPowerEnable => "Track power enable",
            TrackInitStage.NormalOperation => "Normal operation",
            _ => stage.ToString()
        };

        public static readonly TrackInitStage[] All =
        {
            TrackInitStage.ControllerConnection,
            TrackInitStage.FirmwareAddress,
            TrackInitStage.AmplifierStandby,
            TrackInitStage.ConfigureValidate,
            TrackInitStage.GlobalReady,
            TrackInitStage.AmpPowerEnable,
            TrackInitStage.NormalOperation
        };
    }
}
