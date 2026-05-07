using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace PragmaticAnalyzer.Assistant
{
    public class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<string> list)
            {
                string separator = parameter as string ?? ", ";
                return string.Join(separator, list);
            }
            return string.Empty;
        } // преобразовывает список в строку
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack не поддерживается для этого конвертера.");
        }
    }
}
