namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for SiebwaldePage.xaml
    /// </summary>
    public partial class SiebwaldeInitPage : BasePage<SiebwaldeInitPageViewModel>
    {
        public SiebwaldeInitPage()
        {
            InitializeComponent();

            // Keep the automatic host re-detection running only while this page is shown.
            Loaded += (_, __) => (DataContext as SiebwaldeInitPageViewModel)?.StartDetection();
            Unloaded += (_, __) => (DataContext as SiebwaldeInitPageViewModel)?.StopDetection();
        }
    }
}
