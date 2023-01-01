using Home.Helper;
using Home.Model;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Data;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für ScreenshotViewer.xaml
    /// </summary>
    public partial class ScreenshotViewer : UserControl
    {
        private bool isSmall = true;
        private string lastDate = string.Empty;
        private Device lastSelectedDevice = null;

        public delegate void resizeHandler(bool isSmall);
        public event resizeHandler OnResize;

        public event EventHandler OnScreenShotAquired;

        public ScreenshotViewer()
        {
            InitializeComponent();
        }

        #region Image Source

        private void SetImageSource(ImageSource bi)
        {
            ImageViewer.SetImageSource(bi);
        }

        private async Task SetImageSourceAsync(byte[] data, string path, bool grayscale, Device device)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                if (data != null)
                {
                    await ms.WriteAsync(data, 0, data.Length);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                }

                try
                {
                    BitmapImage bi = new BitmapImage();

                    if (data != null)
                        bi = ImageHelper.LoadImage(ms);
                    else
                        bi = ImageHelper.LoadImage(path, true, device.Type == Device.DeviceType.Smartphone);

                    if (grayscale)
                        SetImageSource(ImageHelper.GrayscaleBitmap(bi));
                    else
                        SetImageSource(bi);
                }
                catch (Exception)
                {
                    SetImageSource(null);
                }
            }
        }

        #endregion

        public void UpdateDate(string text)
        {
            if (lastSelectedDevice == null)
                return;

            lastDate = text;

            if (lastSelectedDevice.OS == Device.OSType.Android)
                lastDate = text = $"{lastSelectedDevice.LastSeen.ToString(Properties.Resources.strDateTimeFormat)}";

            if (lastSelectedDevice.IsLive is true)
                TextLive.Text = $"Live - {text}";
            else
                TextLive.Text = $"{text}";
        }

        public async Task UpdateDeviceAsync(Device device)
        {
            if (device == null)
                return;

            bool restoreIndex = false;

            if (lastSelectedDevice?.ID == device.ID)
                restoreIndex = true;

            lastSelectedDevice = device;
            bool enabled = device.Status != Device.DeviceStatus.Offline;
            bool status = device.IsLive ?? false;

            int previousIndex = cmbScreens.SelectedIndex;

            var screens = new List<Screen>();
            if (device.Screens.Count != 1)
                screens.Add(new Screen() { DeviceName = "Default" });
            screens.AddRange(device.Screens);
            cmbScreens.ItemsSource = screens;

            if (previousIndex != -1 && device.Screens.Count > 0 && restoreIndex)
                cmbScreens.SelectedIndex = previousIndex;
            else if (screens.Count > 0)
                cmbScreens.SelectedIndex = 0;

            if (device.Screens.Count == 0)
                cmbScreens.Visibility = Visibility.Collapsed;
            else
                cmbScreens.Visibility = Visibility.Visible;

            UpdateLiveStatus(status, enabled);
            await UpdateScreenshotAsync(device);
        }

        public async Task UpdateScreenshotAsync(Device device)
        {
            // Update the screenshot viewer, respecting the combobox which display is selected 
            if (device == null)
            {
                SetImageSource(null);
                return;
            }

            int selectedIndex = cmbScreens.SelectedIndex;
            bool isOnlyOneScreen = device.Screens.Count == 1;
            bool grayscale = device.Status == Device.DeviceStatus.Offline;
            byte[] data = null;
            string filePath = string.Empty;

            if (device.Screenshots.Count == 0)
            {
                // nothing here
            }
            else if (isOnlyOneScreen)
            {
                if (device.Screenshots.Count > 0)
                {
                    var shot = device.Screenshots.LastOrDefault();
                    if (shot != null)
                        filePath = await DownloadScreenshotAsync(device, shot);
                }
            }
            else if (selectedIndex != 0)
            {
                var shot = device.Screenshots.LastOrDefault(p => p.ScreenIndex == cmbScreens.SelectedIndex - 1); // don't forget item 0 is default and doesn't actually represents a physical screen
                if (shot != null)
                    filePath = await DownloadScreenshotAsync(device, shot);
                else
                {
                    SetImageSource(null);
                    return;
                }
            }
            else
            {
                // Display last screenshot with (ScreenIndex == null) or if not available the last screenshot without (ScreenIndex == null)
                var shot = device.Screenshots.LastOrDefault(p => p.ScreenIndex == null);
                if (shot == null)
                    shot = device.Screenshots.LastOrDefault();

                if (shot == null)
                {
                    SetImageSource(null);
                    return;
                }

                filePath = await DownloadScreenshotAsync(device, shot);
            }

            await SetImageSourceAsync(data, filePath, grayscale, device);
        }

        private async Task<string> DownloadScreenshotAsync(Device device, Screenshot screenshot)
        {
            string path = System.IO.Path.Combine(MainWindow.CACHE_PATH, device.ID, screenshot.Filename) + ".png";
            if (!System.IO.File.Exists(path))
            {
                if (await MainWindow.API.DownloadScreenshotToCache(device, MainWindow.CACHE_PATH, screenshot.Filename))
                {
                    // succuessfully downloaded image to cache
                }
            }

            // Display timestamp and return path
            if (screenshot.Timestamp != null)
                UpdateDate(screenshot.Timestamp.Value.ToString(Properties.Resources.strDateTimeFormat));

            return path;
        }

        private void UpdateToggleButton(bool state, bool enabled)
        {
            ButtonToggleLiveMode.IsEnabled = enabled;
            string image;
            if (enabled)
                image = !state ? "toggle" : "offline";
            else
                image = "offline";     

            string path = $"pack://application:,,,/Home;Component/resources/icons/live/{image}.png";
            ImageToggleLive.Source = ImageHelper.LoadImage(path, false, lastSelectedDevice?.Type == Device.DeviceType.Smartphone);
        }

        private void UpdateLiveImage(bool state, bool enabled)
        {
            string image = state ? "toggle" : "offline";
            if (!state && lastSelectedDevice.Status == Device.DeviceStatus.Active)
                image = "online";

            string path = $"pack://application:,,,/Home;Component/resources/icons/live/{image}.png";
            ImageLive.Source = ImageHelper.LoadImage(path, false, lastSelectedDevice?.Type == Device.DeviceType.Smartphone);
        }

        private void UpdateLiveStatus(bool state, bool enabled)
        {
            UpdateToggleButton(state, enabled);
            UpdateLiveImage(state, enabled);       

            if (state)
            {
                TextLive.Foreground = new SolidColorBrush(Colors.Red);
                TextLive.Text = $"Live - {lastDate}";
            }
            else
            {
                TextLive.Foreground = FindResource("BlackBrush") as SolidColorBrush;
                TextLive.Text = lastDate;
            }
        }

        private void AnimateButton(object sender)
        {
            var sb = FindResource("OnClickAnimation") as Storyboard;
            sb.Begin(sender as FrameworkElement);
        }

        private void ButtonResize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                isSmall = !isSmall;
                OnResize?.Invoke(isSmall);
            }
        }

        private void ButtonRefresh_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                OnScreenShotAquired?.Invoke(sender, EventArgs.Empty);
            }
        }

        private void ButtonSaveImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    AnimateButton(sender);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)ImageViewer.ImageDisplay.Source));

                    SaveFileDialog sfd = new SaveFileDialog { Filter = "Png Bild (*.png)|*.png" };
                    var result = sfd.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        using (System.IO.FileStream stream = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create))
                            encoder.Save(stream);
                    }
                }
                catch (Exception ex)
                {
                    // ToDo: *** Localize
                    MessageBox.Show($"Fehler beim Speichern des Bildes: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ButtonToggleLiveMode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                if (lastSelectedDevice?.Status == Device.DeviceStatus.Offline)
                {
                    // ToDo: *** Localize
                    MessageBox.Show("Das Gerät ist offline - wechseln in den Live Modus nicht möglich!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (lastSelectedDevice.IsLive.HasValue)
                    await MainWindow.API.SetLiveStatusAsync(MainWindow.CLIENT, lastSelectedDevice, !lastSelectedDevice.IsLive.Value);
                else
                    await MainWindow.API.SetLiveStatusAsync(MainWindow.CLIENT, lastSelectedDevice, true);
            }
        }

        private void ButtonResetScrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                ImageViewer.Reset();
            }
        }

        private async void cmbScreens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateScreenshotAsync(lastSelectedDevice);
        }
    }

    #region Converter 

    public class ResolutionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(value?.ToString()))
                return $"({value})";

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Home.Model.Screen sr && sr.DeviceName == "Default")
                return "All";
            else if (value is Home.Model.Screen screen)
                return screen.Index;                

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}