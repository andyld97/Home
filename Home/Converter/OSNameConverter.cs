using System;
using System.Globalization;
using System.Windows.Data;
using static Home.Model.Device;
using static Home.Data.Helper.GeneralHelper;
using Home.Controls;
using Home.Model;

namespace Home.Converter
{
    public class OSNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device d)
            {
                if (d.OS.IsWindows(true))
                    return d.Environment.OSName;
                else 
                    return d.OS.GetDescription();
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
