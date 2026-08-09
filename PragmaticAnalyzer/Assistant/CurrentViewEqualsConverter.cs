using System;
using System.Globalization;
using System.Windows.Data;

namespace PragmaticAnalyzer.Assistant
{
    public class CurrentViewEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Length >= 2 &&
                   values[0] is not null &&
                   ReferenceEquals(values[0], values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
