using Home.Helper;
using Home.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Home.Controls
{
    /// <summary>
    /// Interaction logic for DeviceItemGroup.xaml
    /// </summary>
    public partial class DeviceItemGroup : UserControl, INotifyPropertyChanged
    {
        private string groupName;
        private bool isScreenshotView;
        private bool ignoreSelectionChanged = false;
        private List<Device> devices = new List<Device>();

        public delegate void onGroupSelectionChanged(string groupName);
        public event onGroupSelectionChanged OnGroupSelectionChanged;

        public string GroupName
        {
            get => groupName;
            set
            {
                if (groupName != value)
                {
                    groupName = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Device> Devices
        {
            get => devices;
            set
            {
                if (devices != value)
                {
                    devices = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsScreenshotView
        {
            get => isScreenshotView;
            set
            {
                if (isScreenshotView != value)
                {
                    isScreenshotView = value;
                    OnPropertyChanged();
                }
            }
        }

        public DeviceItemGroup()
        {
            InitializeComponent();
            DataContext = this;            
        }

        #region INotfiyPropertyChanged Impl
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void ListViewDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreSelectionChanged)
                return;

            OnGroupSelectionChanged?.Invoke(GroupName);
        }

        public void ClearSelection()
        {
            ignoreSelectionChanged = true;
            ListViewDevices.SelectedItem = null;
            ignoreSelectionChanged = false;
        }

        public override string ToString()
        {
            return GroupName;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/1585462/bubbling-scroll-events-from-a-listview-to-its-parent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListViewDevices_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }

    #region Converter

    public class DeviceImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device d)
                return ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/devices/{d.DetermineDeviceImage()}", false);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }    
    }

    public class ScreenshotVisibiltyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int.TryParse(parameter.ToString(), out int i);
            if (value is bool b)
            {
                if (b && i == 1)
                    return Visibility.Visible;
                else if (!b && i == 2)
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}