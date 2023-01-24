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
using static Home.Model.DeviceChangeEntry;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für DeviceChanges.xaml
    /// </summary>
    public partial class DeviceChanges : UserControl
    {
        public DeviceChanges()
        {
            InitializeComponent();
        }

        public void Update(Device device)
        {
            ViewHolder.Children.Clear();

            if (device.DevicesChanges.Count == 0)
                return;

            var changes = device.DevicesChanges.GroupBy(p => p.Timestamp);

            foreach (var change in changes)
            {
                ViewHolder.Children.Add(new TextBlock() 
                {
                    Text = $"{change.Key.ToString(Properties.Resources.strDateTimeFormat)}:",
                    Margin = new Thickness(5),
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                });
                ListBox listBox = new ListBox();
                listBox.PreviewMouseWheel += ListBox_PreviewMouseWheel;
                listBox.ItemTemplate = FindResource("DeviceChangeTemplate") as DataTemplate;

                listBox.ItemsSource = change;
                ViewHolder.Children.Add(listBox);
            }    
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // https://stackoverflow.com/a/4342746/6237448
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

    public class DeviceChangeImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DeviceChangeType change)
            {
                string imageSource = "pack://application:,,,/Home;Component/resources/icons/menu/change.png";

                string icon = string.Empty;
                switch (change)
                {
                    case DeviceChangeEntry.DeviceChangeType.CPU: icon = "cpu.png"; break;
                    case DeviceChangeEntry.DeviceChangeType.RAM: icon = "ram.png"; break;
                    case DeviceChangeEntry.DeviceChangeType.Motherboard: icon = "motherboard.png"; break;
                    case DeviceChangeEntry.DeviceChangeType.Graphics: icon = "graphics.png"; break;
                    case DeviceChangeEntry.DeviceChangeType.OS: icon = "menu/change.png"; break;
                    case DeviceChangeEntry.DeviceChangeType.IP: icon = "info.png"; break;
                    case DeviceChangeEntry.DeviceChangeType.DiskDrive: icon = "hdd.png"; break;
                }

                if (!string.IsNullOrEmpty(icon))
                    imageSource = $"pack://application:,,,/Home;Component/resources/icons/{icon}";

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(imageSource);
                bi.EndInit();

                return bi;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
