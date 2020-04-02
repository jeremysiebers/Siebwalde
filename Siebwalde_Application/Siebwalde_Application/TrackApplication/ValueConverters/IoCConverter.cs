using Ninject;
using System;
using System.Diagnostics;
using System.Globalization;

// https://youtu.be/w5kAUCFDRy4?t=4737

namespace Siebwalde_Application
{
    /// <summary>
    /// Converts a string name to a service pulled from the IoC container
    /// </summary>
    public class IoCConverter : BaseValueConverters<IoCConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Find the appropiate page
            switch ((string)parameter)
            {
                case nameof(HmiTrackControlViewModel):
                    return IoC.Get<HmiTrackControlViewModel>();

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
