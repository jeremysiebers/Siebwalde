using Ninject;
using System;

namespace Siebwalde_Application
{
    /// <summary>
    /// The IoC container for our application
    /// </summary>
    public static class IoC
    {
        #region Public properties

        /// <summary>
        /// The kernel for our IoC container
        /// </summary>
        public static IKernel Kernel { get; private set; } = new StandardKernel();

        /// <summary>
        /// A shortcut to access the <see cref="SiebwaldeControlViewModel"/>
        /// </summary>
        public static SiebwaldeControlViewModel SiebwaldeMain => IoC.Get<SiebwaldeControlViewModel>();

        /// <summary>
        /// A shortcut to access the <see cref="TrackApplicationVariables"/>
        /// </summary>
        public static TrackApplicationVariables TrackVar => IoC.Get<TrackApplicationVariables>();

        #endregion

        #region Construction

        /// <summary>
        /// Sets up the IoC container, binds all the information required and is ready for use
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
            Kernel.Bind<SiebwaldeControlViewModel>().ToConstant(new SiebwaldeControlViewModel());

            // bind to a single instance of Track Application Variables
            Kernel.Bind<TrackApplicationVariables>().ToConstant(new TrackApplicationVariables());
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
