namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Very small service locator for the Core library.
    /// This replaces the old Ninject-based IoC for Core.
    /// Only non-UI, cross-cutting services live here (currently: logging).
    /// </summary>
    public static class IoC
    {
        /// <summary>
        /// Global log factory used by all Core classes.
        /// By default it contains a Debug logger.
        /// The host application can override it by calling ConfigureLogger.
        /// </summary>
        public static ILogFactory Logger { get; private set; } = CreateDefaultLogFactory();

        /// <summary>
        /// Allows the host application (e.g. WPF UI) to plug in its own logger
        /// so that Core and UI use the same log pipeline.
        /// </summary>
        /// <param name="logger">The log factory instance to use</param>
        public static void ConfigureLogger(ILogFactory logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates the default log factory that Core uses
        /// when the host application does not override it.
        /// </summary>
        /// <returns>The default <see cref="ILogFactory"/> instance.</returns>
        private static ILogFactory CreateDefaultLogFactory()
        {
            var factory = new BaseLogFactory();

            // Minimal default logging: write to the debugger output window.
            factory.AddLogger(new DebugLogger());

            return factory;
        }
    }
}
