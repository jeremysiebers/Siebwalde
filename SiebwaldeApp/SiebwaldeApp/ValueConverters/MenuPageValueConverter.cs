using System;
using System.Diagnostics;
using System.Globalization;

namespace SiebwaldeApp
{
    /// <summary>
    /// Converts the <see cref="SideMenuPage"/> to an actual view/page
    /// </summary>
    public class MenuPageValueConverter : BaseValueConverter<MenuPageValueConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Find the appropriate page
            switch ((SideMenuPage)value)
            {
                case SideMenuPage.Siebwalde:
                    return new SiebwaldeMenuPage();

                //case ApplicationPage.Register:
                //    return new RegisterPage();

                //case ApplicationPage.Chat:
                //    return new ChatPage();

                default:
                    Debugger.Break();
                    return null;
            }
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
