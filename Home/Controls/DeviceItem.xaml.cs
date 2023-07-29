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
                return ImageHelper.LoadImage(d.GetImage(d.DetermineDeviceImage()));

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

    public class DeviceWarningColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device currentDevice && currentDevice.CountWarnings() > 0)
                return new SolidColorBrush(Colors.Orange);

            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
