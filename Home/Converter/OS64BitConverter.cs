using System;
using System.Globalization;
using System.Windows.Data;

namespace Home.Converter
{
    public class OS64BitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return "64 Bit";

            return "32 Bit";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
