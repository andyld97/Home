using System;
using System.Globalization;
using System.Windows.Data;
using static Home.Model.Device;

namespace Home.Converter
{
    public class OSNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OSType type)
            {
                string result = string.Empty;
                switch (type)
                {
                    case OSType.Linux: result = "Linux"; break;
                    case OSType.LinuxMint: result = "Linux Mint"; break;
                    case OSType.LinuxUbuntu: result = "Ubuntu"; break;
                    case OSType.WindowsXP: result = "Windows XP"; break;
                    case OSType.WindowsaVista: result = "Windows Vista"; break;
                    case OSType.Windows7: result = "Windows 7"; break;
                    case OSType.Windows8: result = "Windows 8"; break;
                    case OSType.Windows10: result = "Windows 10"; break;
                    case OSType.Windows11: result = "Windows 11"; break;
                    case OSType.Unix: result = "Unix"; break;
                    case OSType.Other: result = "Anderes OS"; break;
                    case OSType.Android: result = "Android"; break;
                }

                return result;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
