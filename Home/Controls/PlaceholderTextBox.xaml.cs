using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für PlaceholderTextBox.xaml
    /// </summary>
    public partial class PlaceholderTextBox : UserControl, INotifyPropertyChanged
    {
        private string placeholder = string.Empty;
        private string text = string.Empty;

        public delegate void onTextChanged(object sender, TextChangedEventArgs e);
        public event onTextChanged TextChanged;

        public string Placeholder
        {
            get => placeholder;
            set
            {
                if (placeholder != value)
                {
                    placeholder = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public PlaceholderTextBox()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PlaceholderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = TextBox.Text;
            LabelSearch.Visibility = (string.IsNullOrEmpty(Text) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed);

            TextChanged?.Invoke(sender, e);
        }

        private void ImgClear_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TextBox.Clear();
            Text = string.Empty;
        }
    }
}