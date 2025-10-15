using SiebwaldeApp.Core;

// WPF aliases to avoid clashes with WinForms/System.Drawing
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary>
    /// The View Model for the custom flat window
    /// </summary>
    public class WindowViewModel : BaseViewModel
    {
        #region Private Member

        private Wpf.Window mWindow;
        private WindowResizer mWindowResizer;

        private int mOuterMarginSize = 5;
        private int mWindowRadius = 10;
        private WindowDockPosition mDockPosition = WindowDockPosition.Undocked;

        #endregion

        #region Commands

        public WpfInput.ICommand MinimizeCommand { get; set; }
        public WpfInput.ICommand MaximizeCommand { get; set; }
        public WpfInput.ICommand CloseCommand { get; set; }
        public WpfInput.ICommand MenuCommand { get; set; }

        #endregion

        #region Public Properties

        public double WindowMinimumWidth { get; set; } = 1080;
        public double WindowMinimumHeight { get; set; } = 720;

        public bool Borderless => (mWindow.WindowState == Wpf.WindowState.Maximized || mDockPosition != WindowDockPosition.Undocked);

        public int ResizeBorder => Borderless ? 0 : 3;

        public Wpf.Thickness ResizeBorderThickness => new Wpf.Thickness(ResizeBorder + OuterMarginSize);

        public Wpf.Thickness InnerContentPadding { get; set; } = new Wpf.Thickness(0);

        public int OuterMarginSize
        {
            get => Borderless ? 0 : mOuterMarginSize;
            set => mOuterMarginSize = value;
        }

        public Wpf.Thickness OuterMarginSizeThickness => new Wpf.Thickness(OuterMarginSize);

        public int WindowRadius
        {
            get => Borderless ? 0 : mWindowRadius;
            set => mWindowRadius = value;
        }

        public Wpf.CornerRadius WindowCornerRadius => new Wpf.CornerRadius(WindowRadius);

        public int TitleHeight { get; set; } = 42;

        public Wpf.GridLength TitleHeightGridLength => new Wpf.GridLength(TitleHeight + ResizeBorder);

        #endregion

        #region Constructor

        public WindowViewModel(Wpf.Window window)
        {
            mWindow = window;

            mWindow.StateChanged += (sender, e) => WindowResized();

            mWindowResizer = new WindowResizer(mWindow);

            mWindowResizer.WindowDockChanged += dock =>
            {
                mDockPosition = dock;
                WindowResized();
            };

            MinimizeCommand = new RelayCommand(() => mWindow.WindowState = Wpf.WindowState.Minimized);
            MaximizeCommand = new RelayCommand(() => mWindow.WindowState ^= Wpf.WindowState.Maximized);
            CloseCommand = new RelayCommand(() => mWindow.Close());
            MenuCommand = new RelayCommand(() => Wpf.SystemCommands.ShowSystemMenu(mWindow, GetMousePosition()));
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Gets the current mouse position on the screen
        /// </summary>
        private Wpf.Point GetMousePosition()
        {
            var position = WpfInput.Mouse.GetPosition(mWindow);

            if (mWindow.WindowState == Wpf.WindowState.Maximized)
                return new Wpf.Point(position.X + mWindowResizer.CurrentMonitorSize.Left,
                                     position.Y + mWindowResizer.CurrentMonitorSize.Top);

            return new Wpf.Point(position.X + mWindow.Left, position.Y + mWindow.Top);
        }

        /// <summary>
        /// Fires property change notifications for size-related properties
        /// </summary>
        private void WindowResized()
        {
            OnPropertyChanged(nameof(Borderless));
            OnPropertyChanged(nameof(ResizeBorderThickness));
            OnPropertyChanged(nameof(OuterMarginSize));
            OnPropertyChanged(nameof(OuterMarginSizeThickness));
            OnPropertyChanged(nameof(WindowRadius));
            OnPropertyChanged(nameof(WindowCornerRadius));
        }

        #endregion
    }

    ///// <summary>
    ///// The View Model for the custom flat window
    ///// </summary>
    //public class WindowViewModel : BaseViewModel
    //{
    //    #region Private Member

    //    /// <summary>
    //    /// The window this view model controls
    //    /// </summary>
    //    private Window mWindow;

    //    /// <summary>
    //    /// The window resizer helper that keeps the window size correct in various states
    //    /// </summary>
    //    private WindowResizer mWindowResizer;

    //    /// <summary>
    //    /// The margin around the window to allow for a drop shadow
    //    /// </summary>
    //    private int mOuterMarginSize = 5;

    //    /// <summary>
    //    /// The radius of the edges of the window
    //    /// </summary>
    //    private int mWindowRadius = 10;

    //    /// <summary>
    //    /// The last known dock position
    //    /// </summary>
    //    private WindowDockPosition mDockPosition = WindowDockPosition.Undocked;

    //    #endregion

    //    #region Commands

    //    /// <summary>
    //    /// The command to minimize the window
    //    /// </summary>
    //    public ICommand MinimizeCommand { get; set; }

    //    /// <summary>
    //    /// The command to maximize the window
    //    /// </summary>
    //    public ICommand MaximizeCommand { get; set; }

    //    /// <summary>
    //    /// The command to close the window
    //    /// </summary>
    //    public ICommand CloseCommand { get; set; }

    //    /// <summary>
    //    /// The command to show the system menu of the window
    //    /// </summary>
    //    public ICommand MenuCommand { get; set; }

    //    #endregion

    //    #region Public Properties

    //    /// <summary>
    //    /// The smallest width the window can go to
    //    /// </summary>
    //    public double WindowMinimumWidth { get; set; } = 1080;

    //    /// <summary>
    //    /// The smallest height the window can go to
    //    /// </summary>
    //    public double WindowMinimumHeight { get; set; } = 720;

    //    /// <summary>
    //    /// True if the window should be borderless because it is docked or maximized
    //    /// </summary>
    //    public bool Borderless => (mWindow.WindowState == WindowState.Maximized || mDockPosition != WindowDockPosition.Undocked);
    //    /// <summary>
    //    /// The size of the resize border around the window
    //    /// </summary>
    //    public int ResizeBorder => Borderless ? 0 : 3;

    //    /// <summary>
    //    /// The size of the resize border around the window, taking into account the outer margin
    //    /// </summary>
    //    public Thickness ResizeBorderThickness => new Thickness(ResizeBorder + OuterMarginSize);

    //    /// <summary>
    //    /// The padding of the inner content of the main window
    //    /// </summary>
    //    public Thickness InnerContentPadding { get; set; } = new Thickness(0);

    //    /// <summary>
    //    /// The margin around the window to allow for a drop shadow
    //    /// </summary>
    //    public int OuterMarginSize
    //    {
    //        // If it is maximized or docked, no border
    //        get => Borderless ? 0 : mOuterMarginSize;
    //        set => mOuterMarginSize = value;
    //    }

    //    /// <summary>
    //    /// The margin around the window to allow for a drop shadow
    //    /// </summary>
    //    public Thickness OuterMarginSizeThickness => new Thickness(OuterMarginSize);

    //    /// <summary>
    //    /// The radius of the edges of the window
    //    /// </summary>
    //    public int WindowRadius
    //    {
    //        // If it is maximized or docked, no border
    //        get => Borderless ? 0 : mWindowRadius;
    //        set => mWindowRadius = value;
    //    }

    //    /// <summary>
    //    /// The radius of the edges of the window
    //    /// </summary>
    //    public CornerRadius WindowCornerRadius => new CornerRadius(WindowRadius);

    //    /// <summary>
    //    /// The height of the title bar / caption of the window
    //    /// </summary>
    //    public int TitleHeight { get; set; } = 42;
    //    /// <summary>
    //    /// The height of the title bar / caption of the window
    //    /// </summary>
    //    public GridLength TitleHeightGridLength => new GridLength(TitleHeight + ResizeBorder);

    //    #endregion

    //    #region Constructor

    //    /// <summary>
    //    /// Default constructor
    //    /// </summary>
    //    public WindowViewModel(Window window)
    //    {
    //        mWindow = window;

    //        // Listen out for the window resizing
    //        mWindow.StateChanged += (sender, e) =>
    //        {
    //            // Fire off events for all properties that are affected by a resize
    //            WindowResized();
    //        };

    //        // Fix window resize issue
    //        mWindowResizer = new WindowResizer(mWindow);

    //        // Listen out for dock changes
    //        mWindowResizer.WindowDockChanged += (dock) =>
    //        {
    //            // Store last position
    //            mDockPosition = dock;

    //            // Fire off resize events
    //            WindowResized();
    //        };

    //        // Create commands
    //        MinimizeCommand = new RelayCommand(() => mWindow.WindowState = WindowState.Minimized);
    //        MaximizeCommand = new RelayCommand(() => mWindow.WindowState ^= WindowState.Maximized);
    //        CloseCommand = new RelayCommand(() => mWindow.Close());
    //        MenuCommand = new RelayCommand(() => SystemCommands.ShowSystemMenu(mWindow, GetMousePosition()));
    //    }

    //    #endregion

    //    #region Private Helpers

    //    /// <summary>
    //    /// Gets the current mouse position on the screen
    //    /// </summary>
    //    /// <returns></returns>
    //    private Point GetMousePosition()
    //    {
    //        // Position of the mouse relative to the window
    //        var position = Mouse.GetPosition(mWindow);

    //        // Add the window position so its a "ToScreen"
    //        if (mWindow.WindowState == WindowState.Maximized)
    //            return new Point(position.X + mWindowResizer.CurrentMonitorSize.Left, position.Y + mWindowResizer.CurrentMonitorSize.Top);
    //        else
    //            return new Point(position.X + mWindow.Left, position.Y + mWindow.Top);
    //    }

    //    /// <summary>
    //    /// If the window resizes to a special position (docked or maximized)
    //    /// this will update all required property change events to set the borders and radius values
    //    /// </summary>
    //    private void WindowResized()
    //    {
    //        // Fire off events for all properties that are affected by a resize
    //        OnPropertyChanged(nameof(Borderless));
    //        OnPropertyChanged(nameof(ResizeBorderThickness));
    //        OnPropertyChanged(nameof(OuterMarginSize));
    //        OnPropertyChanged(nameof(OuterMarginSizeThickness));
    //        OnPropertyChanged(nameof(WindowRadius));
    //        OnPropertyChanged(nameof(WindowCornerRadius));
    //    }
    //    #endregion
    //}
}
