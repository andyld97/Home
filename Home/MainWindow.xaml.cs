using Fluent;
using Home.Controls;
using Home.Controls.Dialogs;
using Home.Data;
using Home.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Home
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private readonly Client client = new Client() { IsRealClient = true }; // ToDo: Fixed but unique (just save and load)
        private readonly Home.Communication.API api = null;

        private readonly DispatcherTimer updateTimer = new DispatcherTimer();
        private bool isUpdating = false;
        private readonly object _lock = new object();
        private List<Device> deviceList = new List<Device>();
        private List<DeviceItem> deviceItems = new List<DeviceItem>(); // gui
        private Device lastSelectedDevice = null;

        public MainWindow()
        {
            InitializeComponent();
            api = new Communication.API("http://192.168.178.38:83");
        }

        private async void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Initalize();
        }

        public async Task Initalize()
        {
            var result = await api.LoginAsync(client);
            deviceList = result.Result;

            if (result.Success)
                RefreshDeviceHolder();
            else
                MessageBox.Show(result.ErrorMessage);

            updateTimer.Interval = TimeSpan.FromSeconds(5);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void RefreshDeviceHolder()
        {
            DeviceHolder.Items.Clear();
            deviceItems.Clear();

            foreach (var device in deviceList.OrderBy(p => p.Status))
            {
                DeviceItem di = new DeviceItem() { DataContext = device };
                deviceItems.Add(di);
                DeviceHolder.Items.Add(di);
            }

            RefreshSelection();
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (isUpdating)
                    return;
                else
                    isUpdating = true;
            }

            // Check for event queues ...
            var result = await api.UpdateAsync(client);
            if (result.Success && result.Result != null)
            {
                var device = result.Result;

                if (device.EventDescription == Data.Events.EventQueueItem.EventKind.DeviceScreenshotRecieved)
                {
                    // Get this shot
                    string screenshotFileName = device.EventData.EventDevice.ScreenshotFileNames.LastOrDefault();
                    if (!string.IsNullOrEmpty(screenshotFileName))
                        await GetScreenshot(device.EventData.EventDevice, screenshotFileName);
                }
                else
                {
                    if (deviceList.Any(d => d.ID == device.DeviceID))
                    {
                        // Update 
                        var oldDevice = deviceList.Where(d => d.ID == device.DeviceID).FirstOrDefault();
                        if (oldDevice != null)
                        {
                            // deviceList[deviceList.IndexOf(oldDevice)] = device.EventData.EventDevice;
                            oldDevice.Update(device.EventData.EventDevice, device.EventData.EventDevice.LastSeen, device.EventData.EventDevice.Status, true);

                            if (lastSelectedDevice == oldDevice)
                            {
                                // lastSelectedDevice = device.EventData.EventDevice;
                                //RefreshSelectedItem();
                            }

                            await RefreshSelectedItem();
                            RefreshDeviceHolder();
                        }
                    }
                    else
                    {
                        // Add
                        deviceList.Add(device.EventData.EventDevice);
                        MessageBox.Show("New device added!");

                        RefreshDeviceHolder();
                    }
                }
            }

            lock (_lock)
            {
                isUpdating = false;
            }
        }

        private static readonly string cache_path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");

        private async Task GetScreenshot(Device device, string fileName = "")
        {
            byte[] data = null;
            bool saveInCache = false;
            bool updateGui = (lastSelectedDevice.ID == device.ID);

            string cacheDevicePath = System.IO.Path.Combine(cache_path, device.ID);
            if (!System.IO.Directory.Exists(cacheDevicePath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(cacheDevicePath);
                }
                catch
                {
                    // ToDO: Log
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                // Try to display last-screenshot from cache
                var files = new System.IO.DirectoryInfo(cacheDevicePath).GetFiles();
                System.IO.FileInfo fi = null;

                if (files.Where(p => p.Name == device.ScreenshotFileNames.LastOrDefault()).Any())
                {
                    var file = files.Where(p => p.Name == device.ScreenshotFileNames.LastOrDefault()).LastOrDefault();
                    fi = file;
                }
                else
                    fi = files.LastOrDefault();


                if (fi != null && updateGui)
                {
                    try
                    {
                        data = System.IO.File.ReadAllBytes(fi.FullName);
                        TextLastScreenshotRefresh.Text = $"{fi.LastAccessTime.ToShortDateString()} @ {fi.LastWriteTime.ToShortTimeString()}";
                    }
                    catch
                    {

                    }
                }
                saveInCache = false;
            }
            else
            {
                var recievedScreenshot = await api.RecieveScreenshotAsync(device, fileName);

                if (recievedScreenshot.Success)
                {
                    data = Convert.FromBase64String(recievedScreenshot.Result.Data);
                    saveInCache = true;

                    // Last refresh = now
                    if (updateGui)
                    {
                        var now = DateTime.Now;
                        TextLastScreenshotRefresh.Text = $"{now.ToShortDateString()} @ {now.ToShortTimeString()}";
                    }
                }
            }

            // Save to cache
            if (saveInCache)
            {
                try
                {
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(cacheDevicePath, $"{fileName}.png"), data);
                }
                catch
                {

                }
            }

            if (!updateGui)
                return;

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
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms; 
                    bi.EndInit();


                    if (device.Status == Device.DeviceStatus.Offline)
                    {
                        FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap();
                        grayBitmap.BeginInit();
                        grayBitmap.Source = bi;
                        grayBitmap.DestinationFormat = PixelFormats.Gray8;
                        grayBitmap.EndInit();

                        ImageScreenshot.Source = grayBitmap;
                    }
                    else 
                        ImageScreenshot.Source = bi;
                }
                catch (Exception ex)
                {
                    ImageScreenshot.Source = null;
                }
            }
        }

        private async void DeviceHolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DeviceHolder.SelectedItem is DeviceItem dev)
            {
                lastSelectedDevice = dev.DataContext as Device;
                await RefreshSelectedItem();
                RefreshSelection();
            }
        }

        private void RefreshSelection()
        {
            foreach (var item in deviceItems)
            {
                var ctx = item.DataContext as Device;
                item.SetSelected(lastSelectedDevice?.ID == ctx.ID);
            }
        }

        private async Task RefreshSelectedItem()
        {
            if (lastSelectedDevice == null)
                return;

            TextDeviceLog.Text = string.Join("\n", lastSelectedDevice.LogEntries);
            TextDeviceLog.ScrollToEnd();
            DeviceInfo.DataContext = null;
            DeviceInfo.DataContext = lastSelectedDevice;
            await GetScreenshot(lastSelectedDevice);
        }

        private async void HyperLinkRefreshScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await api.AquireScreenshot(client, lastSelectedDevice);

        }

        private void ImageScreenshot_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {               
                new ScreenshotDialog(ImageScreenshot.Source).ShowDialog();
            }
        }
    }

    #region Converter

    public class DiskImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiskDrive dd)
            {
                string image = string.Empty;
                if (dd.MediaType == "Fixed hard disk media")
                {
                    image = "hdd";
                }
                else if (dd.MediaType == "External hard disk media")
                {
                    image = "usbhdd";
                }
                else if (dd.MediaType == "Removable Media" && dd.DiskInterface == "USB")
                {
                    // Mayebe usb disk or mounted image
                    image = "usb";
                }
                else if (dd.MediaType == "Removable Media" && dd.DiskInterface != "USB")
                {
                    // Maybe CD/DVD
                    image = "cd";
                }

                try
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri($"pack://application:,,,/Home;Component/resources/icons/media/{image}.png");
                    bi.EndInit();

                    return bi;
                }
                catch
                {

                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ByteToGBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ulong bytes)
            {
                double result = bytes / (double)Math.Pow(1024, 3);
                return Math.Round(result, 2);
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SpaceToProgressBarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiskDrive dd)
            {
                double percent = (1.0 - (dd.FreeSpace / (double)dd.TotalSpace)) * 100;
                return (int)Math.Round(percent, 0);
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SpaceToProgressBarColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiskDrive dd)
            {
                double percent = (1.0 - (dd.FreeSpace / (double)dd.TotalSpace)) * 100;
                if (percent <= 50)
                    return new SolidColorBrush(Colors.Lime);
                else if (percent <= 69)
                    return new SolidColorBrush(Colors.Yellow);
                else if (percent <= 80)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }

            return new SolidColorBrush(Colors.Lime);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OS64BitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return "64 Bit";

            return "32 Bit";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    #endregion
}
