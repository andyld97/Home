﻿using Home.Communication;
using Home.Data;
using Home.Data.Helper;
using Home.Data.Remote;
using Home.Helper;
using Home.Model;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Units;

namespace Home.Controls
{
    /// <summary>
    /// Interaction logic for DeviceFileExplorer.xaml
    /// </summary>
    public partial class DeviceFileExplorer : UserControl
    {
        private Device device;
        private RemoteDirectory remoteDirectory;
        private RemoteAPI remoteAPI;

        public delegate void onHomeButtonPressed();
        public event onHomeButtonPressed OnHomeButtonPressed;

        public DeviceFileExplorer()
        {
            InitializeComponent();
            Refresh();
        }

        public async Task PassWebView2Environment(CoreWebView2Environment env)
        {
            await WebViewHTML.EnsureCoreWebView2Async(env);
        }

        public async Task NavigateAsync(Device device, string path)
        {
            if (remoteAPI == null || device != this.device)
                remoteAPI = new RemoteAPI(device.IP, Consts.API_PORT);

            this.device = device;

            if (path.EndsWith(":"))
                path += @"\";

            var result = await remoteAPI.GetRemoteDirectoryAsync(path);
            if (result != null && result.Success)
            {
                remoteDirectory = result.Result;
                Refresh();
            }
            else if (result != null)
            {
                MessageBox.Show($"{Home.Properties.Resources.strFailedToDownloadFolder} {path}!{Environment.NewLine}{Environment.NewLine}{result.ErrorMessage}", Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                OnHomeButtonPressed?.Invoke();
            }
            else
            {
                MessageBox.Show($"{Home.Properties.Resources.strFailedToDownloadFolder} {path}!", Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                OnHomeButtonPressed?.Invoke();
            }
        }

        public void Refresh()
        {
            Data.Items.Clear();

            if (remoteDirectory == null)
                return;

            TextPath.Text = remoteDirectory.Path;

            foreach (var item in remoteDirectory.Directories.OrderBy(p => System.IO.Path.GetFileNameWithoutExtension(p.Path)))
                Data.Items.Add(item);

            foreach (var item in remoteDirectory.Files.OrderBy(p => System.IO.Path.GetFileNameWithoutExtension(p.Path)))
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

                if (remoteDirectory == null)
                    return;

                if (System.IO.Path.GetPathRoot(remoteDirectory.Path).TrimEnd(@"\/".ToCharArray()) == remoteDirectory.Path.TrimEnd(@"\/".ToCharArray()))
                    return;

                string path = System.IO.Path.GetDirectoryName(remoteDirectory.Path);
                // Linux only accepts "/"-paths
                if (!device.OS.IsWindows(false))
                    path = path.Replace(@"\", "/");

                await NavigateAsync(device, path);
            }
        }

        private async void ButtonRefresh_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                if (remoteDirectory == null)
                    return;

                await NavigateAsync(device, remoteDirectory.Path);
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
                    Filter = $"{ext}-{Properties.Resources.strFile}|{ext}",
                    FileName = System.IO.Path.GetFileName(rf.Path)
                };

                var result = sfd.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    var apiResult = await remoteAPI.DownloadFileAsync(rf.Path, sfd.FileName);

                    if (apiResult != null && apiResult.Success)
                        MessageBox.Show(Home.Properties.Resources.strReady, Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                    else  
                        MessageBox.Show($"{Home.Properties.Resources.strFailedToDownloadFile} {rf.Path}!{Environment.NewLine}{Environment.NewLine}{apiResult.ErrorMessage}", Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void MenuDownloadFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Data.SelectedItem is RemoteDirectory rd)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = $"ZIP-{Properties.Resources.strFile}|.zip",
                    FileName = $"{System.IO.Path.GetFileName(rd.Path)}.zip"
                };

                var result = sfd.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    var apiResult = await remoteAPI.DownloadFolderAsync(rd.Path, sfd.FileName);

                    if (apiResult != null && apiResult.Success)
                        MessageBox.Show(Home.Properties.Resources.strReady, Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show($"{Home.Properties.Resources.strFailedToDownloadFile} {rd.Path}!{Environment.NewLine}{Environment.NewLine}{apiResult.ErrorMessage}", Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region Menu Buttons
        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            // ToDo: *** Impl
        }

        private void MenuDeleteDirectory_Click(object sender, RoutedEventArgs e)
        {
            // ToDo: *** Impl
        }

        private void MenuProperties_Click(object sender, RoutedEventArgs e)
        {
            // ToDo: *** Impl
        }

        #endregion

        private async void Data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Data.SelectedItems.Count == 1)
            {
                var selectedItem = Data.SelectedItem;
                bool hidePreview = true;

                if (selectedItem is RemoteFile rf)
                {
                    if (ByteUnit.FromB((long)rf.Length) <= ByteUnit.FromMB(10))
                    {
                        string ext = System.IO.Path.GetExtension(rf.Path).ToLower();

                        if (string.IsNullOrEmpty(ext) || Consts.TEXT_EXTENSIONS.Any(ex => ex == ext))
                        {
                            // Check for empty/text extensions
                            var result = await remoteAPI.DownlaodFileAsync(rf.Path);

                            if (result != null && result.Success)
                            {
                                try
                                {
                                    using (System.IO.StreamReader str = new System.IO.StreamReader(result.Result, System.Text.Encoding.UTF8))
                                        TextFile.Text = await str.ReadToEndAsync();

                                    hidePreview = false;
                                }
                                catch
                                {

                                }
                            }
                            else
                                TextFile.Text = string.Empty;

                            TextFile.Visibility = Visibility.Visible;
                            ImageFile.Visibility = Visibility.Hidden;
                            WebViewHTML.Visibility = Visibility.Hidden;
                        }
                        else if (Consts.IMG_EXTENSIONS.Any(ex => ex == ext))
                        {
                            // Check for image extensions
                            var result = await remoteAPI.DownlaodFileAsync(rf.Path);
                            if (result != null && result.Success)
                            {
                                ImageFile.SetImageSource(ImageHelper.LoadImage(result.Result));
                                ImageFile.Reset();
                                hidePreview = false;
                            }
                            else
                            {
                                ImageFile.SetImageSource(null);
                                ImageFile.Reset();
                            }

                            TextFile.Visibility = Visibility.Hidden;
                            ImageFile.Visibility = Visibility.Visible;
                            WebViewHTML.Visibility = Visibility.Hidden;
                        }
                        else if (Consts.HTML_EXTENSIONS.Any(ex => ex == ext))
                        {
                            var result = await remoteAPI.DownlaodFileAsync(rf.Path);
                            if (result != null && result.Success)
                            {
                                try
                                {
                                    using (System.IO.StreamReader reader = new System.IO.StreamReader(result.Result))
                                    {
                                        string html = reader.ReadToEnd();
                                        WebViewHTML.NavigateToString(html);
                                        hidePreview = false;
                                    }
                                }
                                catch
                                {

                                }
                            }

                            TextFile.Visibility = Visibility.Hidden;
                            ImageFile.Visibility = Visibility.Hidden;
                            WebViewHTML.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            TextFile.Visibility = Visibility.Hidden;
                            ImageFile.Visibility = Visibility.Hidden;
                            WebViewHTML.Visibility = Visibility.Hidden;
                        }

                        if (hidePreview)
                            ColumnPreview.Width = new GridLength(0, GridUnitType.Star);
                        else
                            ColumnPreview.Width = new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        // Clear Preview (preview only shows up to 10 MB per file)
                        ColumnPreview.Width = new GridLength(0, GridUnitType.Star);
                    }
                }
                else
                {
                    // Clear Preview
                    ColumnPreview.Width = new GridLength(0, GridUnitType.Star);
                }
            }
            else
            {
                // Clear Preview
                ColumnPreview.Width = new GridLength(0, GridUnitType.Star);
            }
        }
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
                return ImageHelper.LoadImage("pack://application:,,,/Home;Component/resources/icons/explorer/directory.png", false, false);
            else if (value is RemoteFile rf)
            {
                string icon = "file";
                string ext = System.IO.Path.GetExtension(rf.Path).ToLower();

                if (string.IsNullOrEmpty(ext) || Consts.TEXT_EXTENSIONS.Any(x => x == ext))
                    icon = "text_file";
                else if (Consts.HTML_EXTENSIONS.Any(x => x == ext))
                    icon = "html_file";
                else if (Consts.IMG_EXTENSIONS.Any(x => x == ext))
                    icon = "image_file";

                return ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/explorer/{icon}.png", false, false);
            }

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
                return Properties.Resources.strFolder;
            else if (value is RemoteFile rf)
            {
                string ext = System.IO.Path.GetExtension(rf.Path).ToUpper();
                if (ext.StartsWith("."))
                    ext = ext.Substring(1);

                if (!string.IsNullOrEmpty(ext))
                    return $"{ext}-{Properties.Resources.strFile}";
                else
                    return Properties.Resources.strFile;
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
                return ByteUnit.FindUnit(rf.Length);

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}