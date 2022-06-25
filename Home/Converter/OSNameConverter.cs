using System;
using System.Globalization;
using System.Windows.Data;
using static Home.Model.Device;
using static Home.Data.Helper.GeneralHelper;

namespace Home.Converter
{
    public class OSNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OSType type)
                return type.GetDescription();

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
