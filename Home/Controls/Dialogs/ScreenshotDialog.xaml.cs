using System.Windows;
using System.Windows.Media;

namespace Home.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ScreenshotDialog.xaml
    /// </summary>
    public partial class ScreenshotDialog : Window
    {
        public ScreenshotDialog(ImageSource imageSource)
        {
            InitializeComponent();
            ImageScreenshot.Source = imageSource;
        }
    }
}
