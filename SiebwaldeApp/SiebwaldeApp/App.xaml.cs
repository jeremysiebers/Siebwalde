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
    }
}


//using Ninject;
//using SiebwaldeApp.Core;
//using SiebwaldeApp.Core.Properties;
//using System.IO;
//using System.Windows;
//using Application = System.Windows.Application;
//using MessageBox = System.Windows.MessageBox;

//namespace SiebwaldeApp
//{
//    /// <summary>
//    /// Interaction logic for App.xaml
//    /// </summary>
//    public partial class App : Application
//    {
//        public FiddleYardWinFormViewModel FyWinForm;

//        private const bool USE_SIMULATION = true;

//        /// <summary>
//        /// Custom startup so we load our IoC immediately before anything else
//        /// </summary>
//        protected override void OnStartup(StartupEventArgs e)
//        {
//            // Let the base application do what it needs
//            base.OnStartup(e);

//            // Setup main application
//            ApplicationSetup();

//            if (USE_SIMULATION)
//            {
//                // Load simulation module
//                IoC.Kernel.Load(new SimulationModule());
//                IoC.Logger.Log("Simulation mode active", "");
//            }
//            else
//            {
//                InitializeHardwareAdapters();
//            }

//            // Show the main window
//            Current.MainWindow = new MainWindow();
//            Current.MainWindow.Show();

//        }

//        private void ApplicationSetup()
//        {
//            try
//            {
//                // Verify if logging directory exists
//                if (!Directory.Exists(SiebwaldeApp.Properties.Settings.Default.LogDirectory))
//                {
//                    // Create logging directory
//                    Directory.CreateDirectory(SiebwaldeApp.Properties.Settings.Default.LogDirectory);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(ex.Message);
//            }

//            // Bind a logger
//            IoC.Kernel.Bind<ILogFactory>().ToConstant(new BaseLogFactory());

//            // Bind a file manager
//            IoC.Kernel.Bind<IFileManager>().ToConstant(new FileManager());

//            IoC.Logger.Log("Siebwalde application starting up...", "");

//            // Setup IoC ViewModels, this one as last since the loggers are used within the ViewModels
//            IoC.Setup();

//            FyWinForm = new FiddleYardWinFormViewModel();
//        }

//        private CancellationTokenSource? _udpCts;

//        /// <summary>
//        /// Create and register the PIC adapters (ports) into IoC.
//        /// This must happen before TrackApplication/Station starts.
//        /// </summary>
//        private void InitializeHardwareAdapters()
//        {
//            // 4) Try REAL adapters, fallback to fakes on failure
//            try
//            {
//                IoC.Logger.Log("Initializing REAL hardware adapters...", "");

//                var (trackAdapter, yardAdapter) = UseRealAdaptersOrThrow();

//                _udpCts = new CancellationTokenSource();

//                // Start receive loops on concrete instances (no casts)
//                _ = trackAdapter.StartAsync(_udpCts.Token);
//                _ = yardAdapter.StartAsync(_udpCts.Token);

//                IoC.Logger.Log("REAL hardware adapters initialized.", "");
//            }
//            catch (Exception ex)
//            {
//                IoC.Logger.Log($"REAL adapter init failed, falling back to FAKE. Reason: {ex.Message}", "");
//                UseFakeAdapters();
//            }
//        }

//        private void UseFakeAdapters()
//        {
//            var trackAdapter = new FakeTrackAdapter();
//            var yardAdapter = new FakeYardAdapter();

//            IoC.TrackAdapter.TrackIn = trackAdapter;
//            IoC.TrackAdapter.TrackOut = trackAdapter;
//            IoC.YardAdapter.YardIn = yardAdapter;
//            IoC.YardAdapter.YardOut = yardAdapter;
//        }

//        private (TrackPic18UdpAdapter trackAdapter, YardPic18UdpAdapter yardAdapter) UseRealAdaptersOrThrow()
//        {
//            // Mainline TRACK PIC
//            var trackTransport = new RawUdpTransport("192.168.1.201", 5000);
//            var trackAdapter = new TrackPic18UdpAdapter(trackTransport);

//            // YARD PIC
//            var yardTransport = new RawUdpTransport("192.168.1.202", 5001);
//            var yardAdapter = new YardPic18UdpAdapter(yardTransport);

//            // Register interfaces in IoC
//            IoC.TrackAdapter.TrackIn = trackAdapter;   // ITrackIn
//            IoC.TrackAdapter.TrackOut = trackAdapter;   // ITrackOut

//            IoC.YardAdapter.YardIn = yardAdapter;    // IYardIn
//            IoC.YardAdapter.YardOut = yardAdapter;    // IYardOut

//            // (Optional) sanity check if you use Require* helpers
//            _ = IoC.TrackAdapter.RequireIn();
//            _ = IoC.TrackAdapter.RequireOut();
//            _ = IoC.YardAdapter.RequireIn();
//            _ = IoC.YardAdapter.RequireOut();

//            return (trackAdapter, yardAdapter);
//        }

//        protected override void OnExit(ExitEventArgs e)
//        {
//            try
//            {
//                IoC.siebwaldeApplicationModel.StopTrackApplication();
//                IoC.Logger.Log("Application exiting cleanly.", "");
//            }
//            catch (Exception ex)
//            {
//                IoC.Logger.Log($"Error during shutdown: {ex.Message}", "");
//            }


//            try { _udpCts?.Cancel(); } catch { }

//            IoC.Kernel.Dispose();

//            (IoC.TrackAdapter.TrackIn as IDisposable)?.Dispose();
//            (IoC.TrackAdapter.TrackOut as IDisposable)?.Dispose();
//            (IoC.YardAdapter.YardIn as IDisposable)?.Dispose();
//            (IoC.YardAdapter.YardOut as IDisposable)?.Dispose();
//            base.OnExit(e);
//        }
//    }
//}
