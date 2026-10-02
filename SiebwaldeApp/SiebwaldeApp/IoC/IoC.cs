using Ninject;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using SiebwaldeApp.Integration;

namespace SiebwaldeApp
{
    /// <summary>
    /// The IoC container for our application
    /// </summary>
    public static class IoC
    {
        #region Public Properties

        /// <summary>
        /// The kernel for our IoC container
        /// </summary>
        public static IKernel Kernel { get; private set; } = new StandardKernel();

        /// <summary>
        /// A shortcut to access the <see cref="ApplicationViewModel"/>
        /// </summary>
        public static ApplicationViewModel Application => IoC.Get<ApplicationViewModel>();

        /// <summary>
        /// A shortcut to access the <see cref="SideMenuViewModel"/>
        /// </summary>
        public static SideMenuViewModel SideMenu => IoC.Get<SideMenuViewModel>();

        /// <summary>
        /// A shortcut to access the <see cref="SiebwaldeApp.SiebwaldeApplicationModel"/>
        /// </summary>
        public static SiebwaldeApplicationModel siebwaldeApplicationModel => IoC.Get<SiebwaldeApplicationModel>();

        /// <summary>
        /// A shortcut to access the <see cref="SiebwaldeApp.Core.ITrackApplicationRuntime"/>
        /// coordinator that owns the track-control runtime lifecycle.
        /// </summary>
        public static ITrackApplicationRuntime TrackRuntime => IoC.Get<ITrackApplicationRuntime>();

        /// <summary>
        /// A shortcut to access the <see cref="IFileManager"/>
        /// </summary>
        public static IFileManager File => IoC.Get<IFileManager>();

        /// <summary>
        /// A shortcut to access the <see cref="ILogFactory"/>
        /// </summary>
        public static ILogFactory Logger => IoC.Get<ILogFactory>();              

        #endregion

        #region Construction

        /// <summary>
        /// Sets up the IoC container, binds all information required and is ready for use
        /// NOTE: Must be called as soon as your application starts up to ensure all 
        ///       services can be found
        /// </summary>
        public static void Setup()
        {
            // Bind all required view models
            BindViewModels();
        }

        /// <summary>
        /// Binds all singleton view models
        /// </summary>
        private static void BindViewModels()
        {
            // Bind to a single instance of Application view model
            Kernel.Bind<ApplicationViewModel>().ToConstant(new ApplicationViewModel());

            // Bind to a single instance of Menu view model
            Kernel.Bind<SideMenuViewModel>().ToConstant(new SideMenuViewModel());

            // Register the dedicated production control trace before the control path is
            // composed, so the whole Koploper/ECoS session is captured. It reuses the existing
            // ILogFactory/FileLogger infrastructure and the configured log directory.
            var controlTrace = ControlTraceLogging.Register(
                SiebwaldeApp.Core.IoC.Logger,
                CoreConfiguration.LogDirectory,
                "SiebwaldeApp");

            // Dedicated file-backed diagnostic traces for the EcosEmu <-> Koploper traffic and
            // the occupancy read path. Both reuse the existing ILogFactory/FileLogger
            // infrastructure and the configured log directory; the component name is the logger
            // instance, so each trace lands in its own date-based file.
            var ecosEmuTracePath = EcosEmuTraceLogging.BuildLogFilePath(
                CoreConfiguration.LogDirectory, "SiebwaldeApp.EcosEmu.EcosEmuTrace");
            SiebwaldeApp.Core.IoC.Logger.AddLogger(
                new SiebwaldeApp.Core.FileLogger(ecosEmuTracePath, "SiebwaldeApp.EcosEmu.EcosEmuTrace"));

            var occupancyBridgePath = EcosEmuTraceLogging.BuildLogFilePath(
                CoreConfiguration.LogDirectory, "SiebwaldeApp.Integration.OccupancyBridge");
            SiebwaldeApp.Core.IoC.Logger.AddLogger(
                new SiebwaldeApp.Core.FileLogger(occupancyBridgePath, "SiebwaldeApp.Integration.OccupancyBridge"));

            // The operational amplifier grouping is built once and shared by the ECoS host (which
            // uses it to classify the physical amplifiers) and the runtime coordinator (which uses
            // it as the observed-neutral domain), so both always agree on the domain.
            var amplifierGroups = CoreConfiguration.BuildTrackAmplifierGroups();

            // Load the repository-managed layout profile (best-effort). When it loads, the profile
            // drives the whole control-chain composition (topology, block map, switch mapping,
            // grouping, FullSimulation transport + movement simulator); otherwise the settings-
            // derived defaults are used unchanged. Simple Loop is the default when no profile is
            // selected at runtime.
            FullSimulationProfile.TryLoadByName("Simple Loop", out var profileComposition, out var profileErrors);
            foreach (var profileError in profileErrors)
            {
                SiebwaldeApp.Core.IoC.Logger.Log($"Layout profile problem: {profileError}", "IoC");
            }

            // Bind the ECoS host (the server Koploper connects to on port 15471). Its
            // composition lives in the Integration layer; here we only create it from
            // configuration and hand it to the runtime coordinator.
            IEcosHostService ecosHost;
            if (profileComposition is not null)
            {
                var host = new TrackControlHost(
                    System.IO.Path.Combine(CoreConfiguration.LogDirectory, "locos.json"),
                    profileComposition.BlockTopology,
                    profileComposition.KoploperBlockMap,
                    profileComposition.SwitchMapping,
                    log: message => SiebwaldeApp.Core.IoC.Logger.Log(message, "EcosHost"),
                    trackAmplifierGroups: profileComposition.TrackAmplifierGroups,
                    controlTrace: controlTrace);

                // Make Real mode profile-driven too: bind the profile's REAL physical projection.
                // The host is not running yet, so this is legal. It also re-applies the logical
                // composition (identical to the constructor arguments above).
                host.SetProfile(profileComposition.Profile);

                ecosHost = host;
            }
            else
            {
                ecosHost = TrackControlHost.FromConfiguration(
                    log: message => SiebwaldeApp.Core.IoC.Logger.Log(message, "EcosHost"),
                    controlTrace: controlTrace,
                    trackAmplifierGroups: amplifierGroups);
            }

            Kernel.Bind<IEcosHostService>().ToConstant(ecosHost);

            // The single runtime coordinator owns composition + lifetime of the track-control
            // runtime (Core track part + ECoS host) and exposes the lifecycle to WPF.
            var runtime = new TrackApplicationRuntimeHost(
                ecosHost,
                controlTrace,
                amplifierGroups: profileComposition?.TrackAmplifierGroups ?? amplifierGroups,
                simulatorConfig: profileComposition?.SimulatorConfig,
                fullSimulationProfile: profileComposition?.Profile);
            Kernel.Bind<ITrackApplicationRuntime>().ToConstant(runtime);

            // Bind to a single instance of Siebwalde Application Model
            Kernel.Bind<SiebwaldeApplicationModel>().ToConstant(new SiebwaldeApplicationModel(runtime, controlTrace));
        }

        #endregion

        /// <summary>
        /// Get's a service from the IoC, of the specified type
        /// </summary>
        /// <typeparam name="T">The type to get</typeparam>
        /// <returns></returns>
        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }
    }
}
