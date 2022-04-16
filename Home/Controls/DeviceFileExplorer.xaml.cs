using Home.Data.Helper;
using Home.Data.Remote;
using Home.Helper;
using Home.Model;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Home.Controls
{
    /// <summary>
    /// Interaction logic for DeviceFileExplorer.xaml
    /// </summary>
    public partial class DeviceFileExplorer : UserControl
    {
        private Device device;
        private RemoteDirectory remoteDirectory;

        public delegate void onHomeButtonPressed();
        public event onHomeButtonPressed OnHomeButtonPressed;

        public DeviceFileExplorer()
        {
            InitializeComponent();
            Refresh();
        }

        public async Task NavigateAsync(Device d, string path)
        {
            this.device = d;

            if (path.EndsWith(":"))
                path += @"\";

            string url = $"http://{d.IP}:5556/io/ls/{HttpUtility.UrlEncode(path)}";

            try
            {
                using (HttpClient cl = new HttpClient())
                {
                    var rm = await System.Text.Json.JsonSerializer.DeserializeAsync<RemoteDirectory>(await cl.GetStreamAsync(url));
                    remoteDirectory = rm;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abrufen des Ordners: {path}!{Environment.NewLine}{Environment.NewLine}{ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Refresh();
        }

        public void Refresh()
        {
            Data.Items.Clear();

            if (remoteDirectory == null)
                return;

            TextPath.Text = remoteDirectory.Path;

            foreach (var item in remoteDirectory.Directories)
                Data.Items.Add(item);

            foreach (var item in remoteDirectory.Files)
                Data.Items.Add(item);
        }

        private async void Data_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only navigate when double click directories
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Data.SelectedItem is RemoteDirectory rd)
                    await NavigateAsync(device, rd.Path);
            }
        }

        private void ButtonHome_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                OnHomeButtonPressed?.Invoke();
            }
        }

        private async void ButtonBack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);

                if (System.IO.Path.GetPathRoot(remoteDirectory.Path).TrimEnd(@"\".ToCharArray()) == remoteDirectory.Path.TrimEnd(@"\".ToCharArray()))
                    return;

                string path = System.IO.Path.GetDirectoryName(remoteDirectory.Path);
                await NavigateAsync(device, path);
            }
        }

        private void AnimateButton(object sender)
        {
            var sb = FindResource("OnClickAnimation") as Storyboard;
            sb.Begin(sender as FrameworkElement);
        }

        #region Navigate Buttons
        private async void ButtonNavigate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                await NavigateToPathAsync();
            }
        }

        private async void TextPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await NavigateToPathAsync();
        }

        private async Task NavigateToPathAsync()
        {
            // Prevent to navigate to the same directory multiple times
            if (remoteDirectory != null && TextPath.Text.TrimEnd(@"\/".ToCharArray()) == remoteDirectory.Path.Trim(@"\/".ToCharArray()))
                return;

            await NavigateAsync(device, TextPath.Text);
        }
        #endregion


        #region ContextMenu Download Buttons

        // ToDo: *** Show download progress dialog

        private async void MenuDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if (Data.SelectedItem is RemoteFile rf)
            {
                string ext = System.IO.Path.GetExtension(rf.Path).ToLower();
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = $"{ext}-Datei|{ext}",
                    FileName = System.IO.Path.GetFileName(rf.Path)
                };

                var result = sfd.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    string url = $"http://{device.IP}:5556/io/download/{HttpUtility.UrlEncode(rf.Path)}";

                    try
                    {
                        using (HttpClient cl = new HttpClient())
                        {
                            System.IO.File.WriteAllBytes(sfd.FileName, await cl.GetByteArrayAsync(url));
                            MessageBox.Show("Erfolg!", "Erfolg!", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Herunterladen der Datei: {rf.Path}!{Environment.NewLine}{Environment.NewLine}{ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void MenuDownloadFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Data.SelectedItem is RemoteDirectory rd)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = $"ZIP-Datei|.zip",
                    FileName = System.IO.Path.GetFileName(rd.Path) + ".zip"
                };

                var result = sfd.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    string url = $"http://{device.IP}:5556/io/zip/{HttpUtility.UrlEncode(rd.Path)}";

                    try
                    {
                        using (HttpClient cl = new HttpClient())
                        {
                            System.IO.File.WriteAllBytes(sfd.FileName, await cl.GetByteArrayAsync(url));
                            MessageBox.Show("Erfolg!", "Erfolg!", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Herunterladen der ZIP-Datei des Ordners: {rd.Path}!{Environment.NewLine}{Environment.NewLine}{ex.Message}", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        #endregion
    }

    #region Converter

    public class NameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RemoteDirectory rd)
                return System.IO.Path.GetFileName(rd.Path);
            else if (value is RemoteFile rf)
                return System.IO.Path.GetFileName(rf.Path);

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ImageTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RemoteDirectory)
                return ImageHelper.LoadImage("pack://application:,,,/Home;Component/resources/icons/explorer/directory.png", false);
            else if (value is RemoteFile)
                return ImageHelper.LoadImage("pack://application:,,,/Home;Component/resources/icons/explorer/file.png", false);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RemoteDirectory)
                return "Verzeichnis";
            else if (value is RemoteFile rf)
            {
                string ext = System.IO.Path.GetExtension(rf.Path).ToUpper();
                if (ext.StartsWith("."))
                    ext = ext.Substring(1);

                if (!string.IsNullOrEmpty(ext))
                    return $"{ext}-Datei";
                else
                    return "Datei";
            }

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dt = DateTime.MinValue;
            if (value is RemoteDirectory rd)
                dt = rd.LastChange;
            else if (value is RemoteFile rf)
                dt = rf.LastAccessTime;

            if (value is RemoteDirectory || value is RemoteFile)
                return dt.ToString("G", new CultureInfo("de-DE"));

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RemoteFile rf)
                return ByteUnit.Calculate(rf.Length);

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
