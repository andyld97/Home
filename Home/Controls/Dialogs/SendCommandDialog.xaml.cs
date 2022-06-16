using Home.Communication;
using Home.Model;
using System.Windows;

namespace Home.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für SendCommandDialog.xaml
    /// </summary>
    public partial class SendCommandDialog : Window
    {
        private API apiAccess = null;
        private readonly Device device = null;

        public SendCommandDialog(Device device, API apiAccess)
        {
            InitializeComponent();
            this.device = device;
            this.apiAccess = apiAccess;
            
            Title += $" {Properties.Resources.strTo} {device.Name}";
        }

        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextExecutable.Text))
            {
                MessageBox.Show(this, "Bitte geben Sie gültige Werte ein!", "Ungültige Werte", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await apiAccess.SendCommandAsync(new Data.Com.Command() { DeviceID = device.ID, Executable = TextExecutable.Text, Parameter = TextParameter.Text });
            DialogResult = true;
        }
    }
}
