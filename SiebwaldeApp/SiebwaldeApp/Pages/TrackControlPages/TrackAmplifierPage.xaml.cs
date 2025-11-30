using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary>
    /// Interaction logic for TrackAmplifierPage.xaml
    /// 
    /// Adds mouse wheel behavior:
    /// - Wheel      : vertical scroll (default)
    /// - Shift+Wheel: horizontal scroll
    /// - Ctrl+Wheel : zoom in/out around the mouse cursor
    /// 
    /// Zoom range: 100% (1.0) to 200% (2.0).
    /// </summary>
    public partial class TrackAmplifierPage : BasePage<TrackAmplifierPageViewModel>
    {
        private const double MinZoom = 0.5;
        private const double MaxZoom = 3.0;
        private const double ZoomStep = 0.1;

        private double _currentZoom = 1.0;

        public TrackAmplifierPage()
        {
            InitializeComponent();

            // Attach a PreviewMouseWheel handler so we can customize
            // the wheel behavior based on modifier keys.
            MainScrollViewer.PreviewMouseWheel += MainScrollViewer_PreviewMouseWheel;

            // Handle keyboard input (for Ctrl+0 zoom reset).
            MainScrollViewer.PreviewKeyDown += MainScrollViewer_PreviewKeyDown;

            // Make sure the ScrollViewer can receive keyboard focus.
            MainScrollViewer.Focusable = true;

            // When the page is loaded, give focus to the ScrollViewer
            // so Ctrl+0 works immediately.
            Loaded += (s, e) => MainScrollViewer.Focus();
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ModifierKeys modifiers = Keyboard.Modifiers;

            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl + wheel => zoom at cursor position
                e.Handled = true;
                ZoomAtCursor(e);
            }
            else if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                // Shift + wheel => horizontal scroll
                e.Handled = true;

                // Use the same delta as vertical scroll, but apply it to the horizontal offset.
                double delta = e.Delta; // positive: wheel up
                MainScrollViewer.ScrollToHorizontalOffset(
                    MainScrollViewer.HorizontalOffset - delta);
            }
            else
            {
                // Default behavior: vertical scrolling.
                // Let the ScrollViewer handle it normally by NOT setting e.Handled.
                // If you want custom sensitivity:
                //
                // e.Handled = true;
                // MainScrollViewer.ScrollToVerticalOffset(
                //     MainScrollViewer.VerticalOffset - e.Delta);
            }
        }

        private void MainScrollViewer_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check for Ctrl+0 (both top-row "0" and numpad "0")
            if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control &&
                (e.Key == System.Windows.Input.Key.D0 || e.Key == System.Windows.Input.Key.NumPad0))
            {
                e.Handled = true;
                ResetZoom();
            }
        }


        /// <summary>
        /// Resets the zoom factor back to 100% (1.0).
        /// Keeps the current scroll offsets so the user
        /// stays roughly at the same area in the content.
        /// </summary>
        private void ResetZoom()
        {
            _currentZoom = 1.0;
            ZoomTransform.ScaleX = _currentZoom;
            ZoomTransform.ScaleY = _currentZoom;

            // Ensure the ScrollViewer recalculates its layout
            MainScrollViewer.UpdateLayout();
        }


        /// <summary>
        /// Adjusts the zoom factor while keeping the point under the mouse cursor
        /// approximately at the same position in the viewport.
        /// </summary>
        private void ZoomAtCursor(MouseWheelEventArgs e)
        {
            double oldZoom = _currentZoom;
            double step = e.Delta > 0 ? ZoomStep : -ZoomStep;

            double newZoom = oldZoom + step;
            if (newZoom < MinZoom)
                newZoom = MinZoom;
            if (newZoom > MaxZoom)
                newZoom = MaxZoom;

            if (Math.Abs(newZoom - oldZoom) < 0.001)
                return;

            // Position of the mouse relative to the ScrollViewer (viewport coordinates)
            System.Windows.Point cursorPosInScrollViewer = e.GetPosition(MainScrollViewer);

            // Convert to content coordinates (before zoom change)
            double contentX = (cursorPosInScrollViewer.X + MainScrollViewer.HorizontalOffset) / oldZoom;
            double contentY = (cursorPosInScrollViewer.Y + MainScrollViewer.VerticalOffset) / oldZoom;

            // Apply the new zoom factor
            _currentZoom = newZoom;
            ZoomTransform.ScaleX = _currentZoom;
            ZoomTransform.ScaleY = _currentZoom;

            // Force layout update so Extent/Viewport sizes are up to date
            MainScrollViewer.UpdateLayout();

            // Compute new scroll offsets so that the content point under the cursor
            // stays at the same viewport position.
            double newOffsetX = contentX * _currentZoom - cursorPosInScrollViewer.X;
            double newOffsetY = contentY * _currentZoom - cursorPosInScrollViewer.Y;

            MainScrollViewer.ScrollToHorizontalOffset(newOffsetX);
            MainScrollViewer.ScrollToVerticalOffset(newOffsetY);
        }
    }
}
