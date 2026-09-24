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

            // Bind the ECoS host (the server Koploper connects to on port 15471). Its
            // composition lives in the Integration layer; here we only create it from
            // configuration and hand it to the runtime coordinator.
            var ecosHost = TrackControlHost.FromConfiguration(
                log: message => SiebwaldeApp.Core.IoC.Logger.Log(message, "EcosHost"),
                controlTrace: controlTrace);
            Kernel.Bind<IEcosHostService>().ToConstant(ecosHost);

            // The single runtime coordinator owns composition + lifetime of the track-control
            // runtime (Core track part + ECoS host) and exposes the lifecycle to WPF.
            var runtime = new TrackApplicationRuntimeHost(ecosHost, controlTrace);
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
