using Home.Helper;
using Home.Model;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für DeviceItem.xaml
    /// </summary>
    public partial class DeviceItem : UserControl
    {
        public DeviceItem()
        {
            InitializeComponent();
        }

        public void SetSelected(bool state)
        {
            BorderSelected.Visibility = (state ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed);
        }

        private async void MenuItemReboot_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is Device d)
                await MainWindow.API.ShutdownOrRestartDeviceAsync(false, d);
        }

        private async void MenuItemShutdown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is Device d)
                await MainWindow.API.ShutdownOrRestartDeviceAsync(true, d);
        }
    }

    #region Converter

    public class StateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color fill = Colors.Red;
                 
            if (value is Device.DeviceStatus state)
            {
                switch (state)
                {
                    case Device.DeviceStatus.Active: fill = Colors.Lime; break;
                    case Device.DeviceStatus.Idle: fill = Colors.Yellow; break;
                    case Device.DeviceStatus.Offline: fill = Colors.Red; break;
                }
            }

            return new SolidColorBrush(fill);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TypeToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device dev)
            {
                Color fill = new Color();
              /*  switch (dev.Status)
                {
                    case Device.DeviceStatus.Active: fill = Colors.Lime; break;
                    case Device.DeviceStatus.Idle: fill = Colors.Yellow; break;
                    case Device.DeviceStatus.Offline: fill = Colors.Red; break;
                }*/

                /*if (type == Device.DeviceType.Server)
                    return new SolidColorBrush(Colors.MidnightBlue);
                else if (type == Device.DeviceType.SingleBoardDevice)
                    return new SolidColorBrush(Colors.LimeGreen); */

                return new SolidColorBrush(fill);
            }

            return new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device d)
            {
                string path = $"pack://application:,,,/Home;Component/resources/icons/devices/{d.DetermineDeviceImage()}";
                return ImageHelper.LoadImage(path, false);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviceToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device d && d.Status == Device.DeviceStatus.Offline)
                return 0.6;

            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
