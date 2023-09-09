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
using System.Collections.Concurrent;
using System.Windows.Threading;
using System.ComponentModel;
using System.Drawing;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für ScreenshotViewer.xaml
    /// </summary>
    public partial class ScreenshotViewer : UserControl, INotifyPropertyChanged
    {
        private bool isSmall = true;
        private string lastDate = string.Empty;
        private Device lastSelectedDevice1 = null;
        private bool ignoreCmbScreenSelectionChanged = false;

        #region Download Queue
        private ConcurrentQueue<(Device, Screenshot)> downloadQueue = new ConcurrentQueue<(Device, Screenshot)>();
        private DispatcherTimer queueTimer = new DispatcherTimer();
        private bool isDownloading = false;
        private object sync = new object();
        #endregion

        public delegate void resizeHandler(bool isSmall);
        public event resizeHandler OnResize;

        public event EventHandler OnScreenShotAcquired;
        public event PropertyChangedEventHandler PropertyChanged;

        public Device LastSelectedDevice
        {
            get => lastSelectedDevice1;
            set
            {
                if (value != lastSelectedDevice1)
                {
                    lastSelectedDevice1 = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastSelectedDevice"));
                }
            }
        }

        public ScreenshotViewer()
        {
            InitializeComponent();
            queueTimer.Tick += QueueTimer_Tick;
            queueTimer.Interval = TimeSpan.FromMilliseconds(500);
            queueTimer.Start();
        }

        #region Set Screenshot/Display Screenshot

        private Screenshot screenshotToDisplay = null;

        private async Task SetScreenshotToDisplay(Screenshot value)
        {
            if (screenshotToDisplay != value || value == null)
            {
                screenshotToDisplay = value;

                if (screenshotToDisplay == null)
                {
                    UpdateDate(LastSelectedDevice.LastSeen.ToString(Properties.Resources.strDateTimeFormat));
                    await SetImageSourceAsync(null, string.Empty, (LastSelectedDevice.Type != Device.DeviceType.Smartphone && LastSelectedDevice.Status == Device.DeviceStatus.Offline), LastSelectedDevice);
                }
                else
                {
                    // Display it if it exists (otherwise put it to the queue)
                    var path = GetScreenshotPath(LastSelectedDevice, screenshotToDisplay);

                    if (!string.IsNullOrEmpty(path))
                        await DisplayScreenshotAsync(path, LastSelectedDevice);
                    else
                    {
                        await SetImageSourceAsync(null, string.Empty, (LastSelectedDevice.Type != Device.DeviceType.Smartphone && LastSelectedDevice.Status == Device.DeviceStatus.Offline), LastSelectedDevice);
                        downloadQueue.Enqueue((LastSelectedDevice, value));
                    }
                }
            }
        }

        private async Task DisplayScreenshotAsync(string path, Device device)
        {
            if (screenshotToDisplay == null)
                return;

            await SetImageSourceAsync(null, path, LastSelectedDevice.Status == Device.DeviceStatus.Offline, LastSelectedDevice);
                
            if (screenshotToDisplay.Timestamp != null)
                UpdateDate(screenshotToDisplay.Timestamp.Value.ToString(Properties.Resources.strDateTimeFormat));
        }
        #endregion    

        #region Screenshot Download Queue

        private async void QueueTimer_Tick(object sender, EventArgs e)
        {
            lock (sync)
            {
                if (isDownloading)
                    return;
                else
                    isDownloading = true;
            }

            if (downloadQueue.TryDequeue(out var data))
            {
                var device = data.Item1;
                var screenshot = data.Item2;    

                string path = System.IO.Path.Combine(HomeConsts.CACHE_PATH, device.ID, screenshot.Filename) + ".png";
                if (!System.IO.File.Exists(path))
                {
                    if (await MainWindow.API.DownloadScreenshotToCache(device, HomeConsts.CACHE_PATH, screenshot.Filename))
                    {
                        // Successfully downloaded image to cache
                        if (screenshotToDisplay != null && screenshotToDisplay == screenshot)
                            await DisplayScreenshotAsync(path, device);
                    }
                }
            }

            lock (sync)
                isDownloading = false;
        }

        public void QueueScreenshotDownload(Device device, Screenshot screenshot)
        {
            if (string.IsNullOrEmpty(GetScreenshotPath(device, screenshot)))
                downloadQueue.Enqueue((device, screenshot));
        }

        #endregion

        #region Set Image Source

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

        #region Update
        public void UpdateDate(string text)
        {
            if (LastSelectedDevice == null)
                return;

            lastDate = text;

            if (LastSelectedDevice.OS == Device.OSType.Android)
                lastDate = text = $"{LastSelectedDevice.LastSeen.ToString(Properties.Resources.strDateTimeFormat)}";

            if (LastSelectedDevice.IsLive is true)
                TextLive.Text = $"Live - {text}";
            else
                TextLive.Text = $"{text}";
        }

        public async Task UpdateDeviceAsync(Device device)
        {
            ignoreCmbScreenSelectionChanged = true;
            if (device == null)
                return;

            bool restoreIndex = false;

            if (LastSelectedDevice?.ID == device.ID)
                restoreIndex = true;

            LastSelectedDevice = device;
            bool enabled = device.Status != Device.DeviceStatus.Offline;
            bool status = device.IsLive ?? false;

            int previousIndex = cmbScreens.SelectedIndex;

            var screens = new List<Screen>();
            if (device.Screens.Count != 1)
                screens.Add(new Screen() { DeviceName = Home.Properties.Resources.strDefault });
            screens.AddRange(device.Screens.OrderBy(s => s.Index));
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
            ignoreCmbScreenSelectionChanged = false;
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

            if (device.Screenshots.Count == 0)
                await SetScreenshotToDisplay(null);
            else if (isOnlyOneScreen)
            {
                if (device.Screenshots.Count > 0)
                {
                    var shot = device.Screenshots.OrderBy(s => s.Timestamp).LastOrDefault();
                    if (shot != null)
                        await SetScreenshotToDisplay(shot);
                }
            }
            else if (selectedIndex != 0)
            {
                var shot = device.Screenshots.OrderBy(s => s.Timestamp).LastOrDefault(p => p.ScreenIndex == cmbScreens.SelectedIndex - 1); // don't forget item 0 is default and doesn't actually represents a physical screen
                if (shot != null)
                    await SetScreenshotToDisplay(shot);
                else
                    await SetScreenshotToDisplay(null);
            }
            else
            {
                // Display last screenshot with (ScreenIndex == null) or if not available the last screenshot without (ScreenIndex == null)
                var shot = device.Screenshots.OrderBy(s => s.Timestamp).LastOrDefault(p => p.ScreenIndex == null);
                if (shot != null)
                    await SetScreenshotToDisplay(shot);
            }
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
            ImageToggleLive.Source = ImageHelper.LoadImage(path, false, LastSelectedDevice?.Type == Device.DeviceType.Smartphone);
        }

        private void UpdateLiveImage(bool state, bool enabled)
        {
            string image = state ? "toggle" : "offline";
            if (!state && LastSelectedDevice.Status == Device.DeviceStatus.Active)
                image = "online";

            string path = $"pack://application:,,,/Home;Component/resources/icons/live/{image}.png";
            ImageLive.Source = ImageHelper.LoadImage(path, false, LastSelectedDevice?.Type == Device.DeviceType.Smartphone);
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
                TextLive.Foreground = FindResource("Fluent.Ribbon.Brushes.Black") as SolidColorBrush;
                TextLive.Text = lastDate;
            }
        }
        #endregion

        #region Events / Helper Methods
        private string GetScreenshotPath(Device device, Screenshot screenshot)
        {
            string path = System.IO.Path.Combine(HomeConsts.CACHE_PATH, device.ID, screenshot.Filename) + ".png";
            if (System.IO.File.Exists(path))
                return path;

            return string.Empty;
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
                OnScreenShotAcquired?.Invoke(sender, EventArgs.Empty);
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

                    SaveFileDialog sfd = new SaveFileDialog { Filter = Home.Properties.Resources.strPngFilter };
                    var result = sfd.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        using (System.IO.FileStream stream = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create))
                            encoder.Save(stream);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Home.Properties.Resources.strFailedToSaveScreenshotAsImage, ex.Message), Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ButtonToggleLiveMode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AnimateButton(sender);
                if (LastSelectedDevice?.Status == Device.DeviceStatus.Offline)
                {
                    MessageBox.Show(Home.Properties.Resources.strDeviceCannotSwitchToLiveMode, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (LastSelectedDevice.IsLive == true)
                    await MainWindow.API.SetLiveStatusAsync(MainWindow.CLIENT, LastSelectedDevice, !LastSelectedDevice.IsLive.Value);
                else
                    await MainWindow.API.SetLiveStatusAsync(MainWindow.CLIENT, LastSelectedDevice, true);
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
            if (ignoreCmbScreenSelectionChanged)
                return;
                
            await UpdateScreenshotAsync(LastSelectedDevice);
        }
        #endregion
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
            if (value is Home.Model.Screen sr && sr.DeviceName == Properties.Resources.strDefault)
                return Home.Properties.Resources.strAll;
            else if (value is Home.Model.Screen screen)
                return screen.Index;                

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ScreenIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Offline and Online icon
            string path = $"pack://application:,,,/Home;Component/resources/icons/";

            if (value is Device d && d.Status == Device.DeviceStatus.Active)
                path += "screen.png";
            else
                path += "screen_offline.png";

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path);
            bi.EndInit();

            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}