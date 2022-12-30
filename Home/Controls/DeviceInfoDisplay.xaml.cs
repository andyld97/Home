using Home.Data;
using Home.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für DeviceInfoDisplay.xaml
    /// </summary>
    public partial class DeviceInfoDisplay : UserControl
    {
        public DeviceInfoDisplay()
        {
            InitializeComponent();
        }

        public void UpdateDevice(Device currentDevice)
        {
            DataContext = currentDevice;
            CmbGraphics.SelectedIndex = 0;
        }
    }

    #region Converter
    public class ServiceVersionTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ToDo: *** This won't update if the theme will be changed
            if (value is null || string.IsNullOrEmpty(value.ToString()))
                return Application.Current.FindResource("BlackBrush");

            string[] versions = { $"vWindows{Consts.HomeServiceWindowsClientVersion}", $"vLinux{Consts.HomeServiceLinuxClientVersion}", $"vAndroid{Consts.HomeServiceAndroidClientVersion}", $"vLegacy{Consts.HomeServiceLegacyClientVersion}" };
            if (!versions.Any(v => v == value.ToString()))
                return new SolidColorBrush(Colors.Yellow);

            return Application.Current.FindResource("BlackBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}