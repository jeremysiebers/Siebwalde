using System;

using Siebwalde_Application2.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Siebwalde_Application2.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
