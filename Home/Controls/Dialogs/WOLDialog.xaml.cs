using Home.Data.Helper;
using Home.Model;
using System.Linq;
using System.Windows;

namespace Home.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für WOLDialog.xaml
    /// </summary>
    public partial class WOLDialog : Window
    {
        public WOLDialog(string deviceID = "")
        {
            InitializeComponent();
            RefreshDevices(string.Empty, deviceID);
        }

        private void RefreshDevices(string search = "", string deviceID = "")
        {
            var devices = MainWindow.W_INSTANCE.GetDevices();
            devices = devices.Where(p => p.OS.IsWindows(true) || p.OS.IsLinux()).ToList();

            // Apply search text (if any)
            if (!string.IsNullOrEmpty(search))
                devices = devices.Where(d => d.Name.ToLower().Contains(search.ToLower())); 

            CmbDevices.ItemsSource = devices;

            if (devices.Any())
            {
                CmbDevices.SelectedIndex = 0;

                if (!string.IsNullOrEmpty(deviceID) && devices.Any(d => d.ID == deviceID))
                    CmbDevices.SelectedItem = devices.FirstOrDefault(p => p.ID ==  deviceID);

                CmbDevices.Visibility = Visibility.Visible;
                TextNoDevices.Visibility = Visibility.Collapsed;
            }
            else
            {
                CmbDevices.Visibility = Visibility.Collapsed;
                TextNoDevices.Visibility = Visibility.Visible;
            }
        }

        private void CmbDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbDevices.SelectedItem is Device device)
                TextMac.Text = device.MacAddress;
        }

        private async void ButtonSendMagicPackage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextMac.Text))
            {
                MessageBox.Show(Properties.Resources.strWakeOnLanDialog_InvalidMac, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = await MainWindow.API.WakeOnLanAsync(TextMac.Text);
            if (result.Success)
                DialogResult = true;
            else
               MessageBox.Show(Properties.Resources.strWakeOnLanDialog_FailedToSendWOLRequest, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void TextSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            RefreshDevices(TextSearch.Text);
        }
    }
}