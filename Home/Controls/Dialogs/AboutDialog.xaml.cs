using System.Windows;

namespace Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();

            TextDotNetVersion.Text = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            TextVersion.Text = typeof(AboutDialog).Assembly.GetName().Version.ToString(3);

            if (Home.Properties.Resources.strLang == "de")
                TextRelease.Text += " Uhr";
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Home.Helper.GeneralHelper.OpenUri(e.Uri);
        }
    }
}
