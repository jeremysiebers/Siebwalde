using System;
using System.Diagnostics;
using System.Globalization;
using SiebwaldeApp.Core;

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
                    return new SiebwaldeMenu();

                case SideMenuPage.TrackControl:
                    return new TrackMenu();

                case SideMenuPage.FiddleYardControl:
                    return new FiddleYardMenu();

                case SideMenuPage.YardControl:
                    return new YardMenu();

                case SideMenuPage.CityControl:
                    return new CityMenu();

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
