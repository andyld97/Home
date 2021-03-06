using Home.Communication;
using Home.Model;
using System.Windows;

namespace Home.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für SendMessage.xaml
    /// </summary>
    public partial class SendMessage : Window
    {
        private API apiAccess = null;
        private Device device = null;

        public SendMessage(Device device, API apiAccess)
        {
            InitializeComponent();
            this.device = device;
            this.apiAccess = apiAccess;

            Title += $" an {device.Name}";
        }

        private void ComboBox_Selected(object sender, RoutedEventArgs e)
        {

        }

        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            // ToDo: Check if values != ""

            if (string.IsNullOrEmpty(TextMessage.Text) || string.IsNullOrEmpty(TextTitle.Text))
            {
                MessageBox.Show("Bitte geben Sie gültige Werte ein!", "Leere Werte!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = await apiAccess.SendMessageAsync(new Data.Com.Message(TextMessage.Text, TextTitle.Text, (Data.Com.Message.MessageImage)CmbType.SelectedIndex) { DeviceID = device.ID });
            this.DialogResult = true;
        }
    }
}
