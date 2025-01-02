using System.Windows;

namespace Home.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für BroadcastShutdownDialog.xaml
    /// </summary>
    public partial class BroadcastShutdownDialog : Window
    {
        public BroadcastShutdownDialog()
        {
            InitializeComponent();
        }

        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PasswordCode.Password))
            {
                MessageBox.Show(Home.Properties.Resources.strBroadcastShutdown_ValidPassword, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(TextReason.Text))
            {
                MessageBox.Show(Home.Properties.Resources.strBroadcastShutdown_ValidReason, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool shutdownAllDevices = ChkShutdownAll.IsChecked.Value;
            var result = await MainWindow.API.BroadcastShutdownAsync(MainWindow.CLIENT, PasswordCode.Password, TextReason.Text, shutdownAllDevices);

            if (result.Success)
            {
                MessageBox.Show(Home.Properties.Resources.strBroadcastShutdown_SuccessMessage, Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
                MessageBox.Show(result.ErrorMessage, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
