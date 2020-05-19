using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace SiebwaldeApp
{
    /// <summary>
    /// A converter that takes in a observable collection and returns <see cref="Text"/>
    /// </summary>
    public class EnumarableToTextConverter : MultiBaseValueConverter<EnumarableToTextConverter>
    {
        //public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    if (value is IEnumerable)
        //    {



        //        StringBuilder sb = new StringBuilder();
        //        foreach (var s in value as IEnumerable)
        //        {
        //            sb.AppendLine(s.ToString());                  
        //        }
        //        return sb.ToString();
        //    }
        //    return string.Empty;
        //}

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is ObservableCollection<string> log && log.Count > 0)
            {
                return log;
            }
            else
            {
                return String.Empty;
            }
        }

        //public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    throw new NotImplementedException();
        //}

        public override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
