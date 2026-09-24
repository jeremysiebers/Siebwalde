using System.IO;
using Ninject;
using System.Windows;
using Application = System.Windows.Application;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public FiddleYardWinFormViewModel FyWinForm;

        /// <summary>
        /// Custom startup so we load our IoC immediately before anything else
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Let the base application do what it needs
            base.OnStartup(e);

            // Setup main application
            ApplicationSetup();

            // Show the main window
            Current.MainWindow = new MainWindow();
            Current.MainWindow.Show();
        }

        private void ApplicationSetup()
        {
            try
            {
                // Verify if logging directory exists
                if (!Directory.Exists(SiebwaldeApp.Properties.Settings.Default.LogDirectory))
                {
                    // Create logging directory
                    Directory.CreateDirectory(SiebwaldeApp.Properties.Settings.Default.LogDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

            // Bind a logger
            IoC.Kernel.Bind<ILogFactory>().ToConstant(new BaseLogFactory());

            // Bind a file manager
            IoC.Kernel.Bind<IFileManager>().ToConstant(new FileManager());

            IoC.Logger.Log("Siebwalde application starting up...", "");

            // Setup IoC ViewModels, this one as last since the loggers are used within the ViewModels
            IoC.Setup();

            FyWinForm = new FiddleYardWinFormViewModel();
        }

        /// <summary>
        /// Best-effort graceful stop on exit, bounded so application shutdown cannot hang.
        /// The process exit remains the final fallback, but the runtime stop is initiated (and,
        /// in the normal case, completed) here instead of relying solely on process termination.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                IoC.TrackRuntime.StopAsync(cts.Token).GetAwaiter().GetResult();
            }
            catch
            {
                // Best-effort: process exit remains the final fallback.
            }
        }
    }
}
