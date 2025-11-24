using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// 👇 Alias WPF to avoid clashes with System.Drawing
using Wpf = System.Windows;
using WpfInterop = System.Windows.Interop;
using WpfMedia = System.Windows.Media;

namespace SiebwaldeApp
{
    /// <summary>
    /// The dock position of the window
    /// </summary>
    public enum WindowDockPosition
    {
        Undocked,
        Left,
        Right,
    }

    /// <summary>
    /// Fixes the issue with Windows of Style <see cref="Wpf.WindowStyle.None"/> covering the taskbar
    /// </summary>
    public class WindowResizer
    {
        #region Private Members

        private Wpf.Window mWindow;

        private Wpf.Rect mScreenSize = new Wpf.Rect();

        private int mEdgeTolerance = 2;

        private WpfMedia.Matrix mTransformToDevice;

        private IntPtr mLastScreen;

        private WindowDockPosition mLastDock = WindowDockPosition.Undocked;

        #endregion

        #region Dll Imports

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

        #endregion

        #region Public Events

        /// <summary>Called when the window dock position changes</summary>
        public event Action<WindowDockPosition> WindowDockChanged = _ => { };

        #endregion

        #region Public Properties

        /// <summary>The size and position of the current monitor the window is on</summary>
        public Rectangle CurrentMonitorSize { get; set; } = new Rectangle();

        #endregion

        #region Constructor

        public WindowResizer(Wpf.Window window)
        {
            mWindow = window;

            // Create transform visual (for converting WPF size to pixel size)
            GetTransform();

            // Listen out for source initialized to setup
            mWindow.SourceInitialized += Window_SourceInitialized;

            // Monitor for edge docking
            mWindow.SizeChanged += Window_SizeChanged;
        }

        #endregion

        #region Initialize

        /// <summary>Gets the transform object used to convert WPF sizes to screen pixels</summary>
        private void GetTransform()
        {
            // Get the visual source
            var source = Wpf.PresentationSource.FromVisual(mWindow);

            // Reset the transform to default
            mTransformToDevice = default(WpfMedia.Matrix);

            // If we cannot get the source, ignore
            if (source is null)
                return;

            // Otherwise, get the new transform object
            mTransformToDevice = source.CompositionTarget.TransformToDevice;
        }

        /// <summary>Initialize and hook into the windows message pump</summary>
        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            // Get the handle of this window
            var handle = (new WpfInterop.WindowInteropHelper(mWindow)).Handle;
            var handleSource = WpfInterop.HwndSource.FromHwnd(handle);

            if (handleSource is null)
                return;

            // Hook into its Windows messages
            handleSource.AddHook(WindowProc);
        }

        #endregion

        #region Edge Docking

        /// <summary>Monitors for size changes and detects if the window has been docked (Aero snap) to an edge</summary>
        private void Window_SizeChanged(object? sender, Wpf.SizeChangedEventArgs e)
        {
            // We cannot find positioning until the window transform has been established
            if (mTransformToDevice == default(WpfMedia.Matrix))
                return;

            // Get the WPF size
            var size = e.NewSize;

            // Get window rectangle
            var top = mWindow.Top;
            var left = mWindow.Left;
            var bottom = top + size.Height;
            var right = left + mWindow.Width;

            // Get window position/size in device pixels (WPF Point)
            var windowTopLeft = mTransformToDevice.Transform(new Wpf.Point(left, top));
            var windowBottomRight = mTransformToDevice.Transform(new Wpf.Point(right, bottom));

            // Check for edges docked
            var edgedTop = windowTopLeft.Y <= (mScreenSize.Top + mEdgeTolerance);
            var edgedLeft = windowTopLeft.X <= (mScreenSize.Left + mEdgeTolerance);
            var edgedBottom = windowBottomRight.Y >= (mScreenSize.Bottom - mEdgeTolerance);
            var edgedRight = windowBottomRight.X >= (mScreenSize.Right - mEdgeTolerance);

            // Get docked position
            var dock = WindowDockPosition.Undocked;

            if (edgedTop && edgedBottom && edgedLeft)
                dock = WindowDockPosition.Left;
            else if (edgedTop && edgedBottom && edgedRight)
                dock = WindowDockPosition.Right;

            if (dock != mLastDock)
                WindowDockChanged(dock);

            mLastDock = dock;
        }

        #endregion

        #region Windows Proc

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                // WM_GETMINMAXINFO
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        #endregion

        /// <summary>
        /// Get the min/max window size for this window, correctly accounting for the taskbar
        /// </summary>
        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            // Get the point position to determine what screen we are on
            GetCursorPos(out POINT lMousePosition);

            // Get the primary monitor at cursor position 0,0
            var lPrimaryScreen = MonitorFromPoint(new POINT(0, 0), MonitorOptions.MONITOR_DEFAULTTOPRIMARY);

            // Try and get the primary screen information
            var lPrimaryScreenInfo = new MONITORINFO();
            if (!GetMonitorInfo(lPrimaryScreen, lPrimaryScreenInfo))
                return;

            // Now get the current screen
            var lCurrentScreen = MonitorFromPoint(lMousePosition, MonitorOptions.MONITOR_DEFAULTTONEAREST);

            // If this has changed from the last one, update the transform
            if (lCurrentScreen != mLastScreen || mTransformToDevice == default(WpfMedia.Matrix))
                GetTransform();

            // Store last known screen
            mLastScreen = lCurrentScreen;

            // Get min/max structure to fill with information
            var lMmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;

            // If it is the primary screen, use the rcWork variable
            if (lPrimaryScreen.Equals(lCurrentScreen))
            {
                lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcWork.Left;
                lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcWork.Top;
                lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcWork.Right - lPrimaryScreenInfo.rcWork.Left;
                lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcWork.Bottom - lPrimaryScreenInfo.rcWork.Top;
            }
            else
            {
                lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcMonitor.Left;
                lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcMonitor.Top;
                lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcMonitor.Right - lPrimaryScreenInfo.rcMonitor.Left;
                lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcMonitor.Bottom - lPrimaryScreenInfo.rcMonitor.Top;
            }

            // Set monitor size
            CurrentMonitorSize = new Rectangle(lMmi.ptMaxPosition.X, lMmi.ptMaxPosition.Y,
                                               lMmi.ptMaxSize.X + lMmi.ptMaxPosition.X,
                                               lMmi.ptMaxSize.Y + lMmi.ptMaxPosition.Y);

            // Set min size (WPF Point)
            var minSize = mTransformToDevice.Transform(new Wpf.Point(mWindow.MinWidth, mWindow.MinHeight));
            lMmi.ptMinTrackSize.X = (int)minSize.X;
            lMmi.ptMinTrackSize.Y = (int)minSize.Y;

            // Store new size
            mScreenSize = new Wpf.Rect(lMmi.ptMaxPosition.X, lMmi.ptMaxPosition.Y, lMmi.ptMaxSize.X, lMmi.ptMaxSize.Y);

            // Write back
            Marshal.StructureToPtr(lMmi, lParam, true);
        }
    }

    #region Dll Helper Structures

    enum MonitorOptions : uint
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MONITORINFO
    {
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        public Rectangle rcMonitor = new Rectangle();
        public Rectangle rcWork = new Rectangle();
        public int dwFlags = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
        public int Left, Top, Right, Bottom;

        public Rectangle(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    #endregion
}
