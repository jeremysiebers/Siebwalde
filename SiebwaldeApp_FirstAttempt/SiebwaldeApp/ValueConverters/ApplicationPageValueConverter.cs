using SiebwaldeApp.Core;
using System;
using System.Diagnostics;
using System.Globalization;

namespace SiebwaldeApp
{
    /// <summary>
    /// Converts the <see cref="ApplicationPage"/> to an actual view/page
    /// </summary>
    public class ApplicationPageValueConverter : BaseValueConverter<ApplicationPageValueConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Find the appropriate page
            switch ((ApplicationPage)value)
            {
                /*------------- MainWindow ------------------*/
                case ApplicationPage.Siebwalde:
                    return new SiebwaldePage();

                case ApplicationPage.TrackControl:
                    return new TrackPage();

                case ApplicationPage.FiddleYardControl:
                    return new FiddleYardPage();

                case ApplicationPage.YardControl:
                    return new YardPage();

                case ApplicationPage.CityControl:
                    return new CityPage();

                /*------------- SiebwaldeControl --------------*/

                case ApplicationPage.SiebwaldeInit:
                    return new SiebwaldeInitPage();

                case ApplicationPage.SiebwaldeSettings:
                    return new SiebwaldeSettingsPage();

                /*------------- TrackControl ------------------*/

                case ApplicationPage.TrackAmplifier:
                    return new TrackAmplifierPage();

                    
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
