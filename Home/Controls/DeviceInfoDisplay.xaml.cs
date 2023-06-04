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

            // See ServiceVersionTextColorConverter: It the color is not set dynamically so if you change the theme in the settings to light/dark mode it needs
            // to be updated that it will be shown properly
            currentDevice.OnPropertyChanged(nameof(currentDevice.ServiceClientVersion));

            if (currentDevice.Environment.GraphicCards.Count > 1)
            {
                CmbGraphics.Visibility = Visibility.Visible;
                TextGraphics.Visibility = Visibility.Collapsed;
            }
            else
            {
                CmbGraphics.Visibility = Visibility.Collapsed;
                string graphics = currentDevice.Environment.GraphicCards.FirstOrDefault();
                if (string.IsNullOrEmpty(graphics))
                    graphics = Home.Properties.Resources.strUnkown;
                TextGraphics.Text = graphics;
                TextGraphics.ToolTip = graphics;
                TextGraphics.Visibility = Visibility.Visible;
            }
        }
    }

    #region Converter
    public class ServiceVersionTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ToDo: *** This won't update if the theme will be changed
            if (value is null || string.IsNullOrEmpty(value.ToString()))
                return Application.Current.FindResource("Black");

            string[] versions = { $"vWindows{Consts.HomeServiceWindowsClientVersion}", $"vLinux{Consts.HomeServiceLinuxClientVersion}", $"vAndroid{Consts.HomeServiceAndroidClientVersion}", $"vLegacy{Consts.HomeServiceLegacyClientVersion}" };
            if (!versions.Any(v => v == value.ToString()))
                return new SolidColorBrush(Colors.Orange);

            return Application.Current.FindResource("Fluent.Ribbon.Brushes.Black");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ServiceVersionToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || string.IsNullOrEmpty(value.ToString()))
                return string.Empty;

            string[] versions = { $"vWindows{Consts.HomeServiceWindowsClientVersion}", $"vLinux{Consts.HomeServiceLinuxClientVersion}", $"vAndroid{Consts.HomeServiceAndroidClientVersion}", $"vLegacy{Consts.HomeServiceLegacyClientVersion}" };
            if (!versions.Any(v => v == value.ToString()))
                return Home.Properties.Resources.strClientVersionNotUpToDate;

            return Home.Properties.Resources.strClientVersionUpToDate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}