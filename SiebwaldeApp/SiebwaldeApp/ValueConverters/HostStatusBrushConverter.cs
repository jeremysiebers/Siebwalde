using System;
using System.Globalization;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace SiebwaldeApp
{
    /// <summary>
    /// Converts the host presence flag and the page-level detecting flag into the brush
    /// for a host status dot. Expected MultiBinding values, in order:
    /// [0] IsPresent (bool), [1] IsDetecting (bool).
    /// Checking (amber) takes precedence over present (green); the default is the
    /// absent/unknown gray. The brushes are resolved from the application resources so
    /// the indicator stays consistent with the rest of the application.
    /// </summary>
    public class HostStatusBrushConverter : MultiBaseValueConverter<HostStatusBrushConverter>
    {
        private static readonly MediaBrush FallbackChecking = new MediaSolidColorBrush(MediaColor.FromRgb(0xFF, 0xA8, 0x00));
        private static readonly MediaBrush FallbackPresent = new MediaSolidColorBrush(MediaColor.FromRgb(0x00, 0xC5, 0x41));
        private static readonly MediaBrush FallbackAbsent = new MediaSolidColorBrush(MediaColor.FromRgb(0xBD, 0xBD, 0xBD));

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var present = values is { Length: > 0 } && values[0] is bool presentValue && presentValue;
            var detecting = values is { Length: > 1 } && values[1] is bool detectingValue && detectingValue;

            if (detecting)
            {
                return Resolve("WordOrangeBrush", FallbackChecking);
            }

            if (present)
            {
                return Resolve("WordGreenBrush", FallbackPresent);
            }

            return Resolve("ForegroundDarkBrush", FallbackAbsent);
        }

        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static MediaBrush Resolve(string resourceKey, MediaBrush fallback)
            => System.Windows.Application.Current?.TryFindResource(resourceKey) as MediaBrush ?? fallback;
    }
}
