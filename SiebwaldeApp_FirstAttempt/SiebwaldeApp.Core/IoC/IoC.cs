using Ninject;

namespace SiebwaldeApp.Core
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
        /// A shortcut to access the <see cref="IFileManager"/>
        /// </summary>
        public static IFileManager File => IoC.Get<IFileManager>();

        /// <summary>
        /// A shortcut to access the <see cref="ILogFactory"/>
        /// </summary>
        public static ILogFactory Logger => IoC.Get<ILogFactory>();

        /// <summary>
        /// A shortcut to access the <see cref="TrackApplicationVariables"/>
        /// </summary>
        //public static TrackApplicationVariables TrackVariables => IoC.Get<TrackApplicationVariables>(); OLD

        // Shared adapters (ports)
        public static class TrackAdapter
        {
            public static ITrackIn? TrackIn { get; set; }
            public static ITrackOut? TrackOut { get; set; }

            // Safe accessors that fail fast if not initialized
            public static ITrackIn RequireIn() => TrackIn ?? (TrackIn = IoC.Kernel.Get<ITrackIn>());
            public static ITrackOut RequireOut() => TrackOut ?? (TrackOut = IoC.Kernel.Get<ITrackOut>());
        }
    

        public static class YardAdapter
        {
            public static IYardIn? YardIn { get; set; }
            public static IYardOut? YardOut { get; set; }

            public static IYardIn RequireIn() => YardIn ?? (YardIn = IoC.Kernel.Get<IYardIn>());
            public static IYardOut RequireOut() => YardOut ?? (YardOut = IoC.Kernel.Get<IYardOut>());
        }

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

            // Bind to a single instance of Siebwalde Application Model
            Kernel.Bind<SiebwaldeApplicationModel>().ToConstant(new SiebwaldeApplicationModel());

            // bind to a single instance of Track Application Variables
            //Kernel.Bind<TrackApplicationVariables>().ToConstant(new TrackApplicationVariables()); OLD
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
