﻿using Fluent;
using Home.Controls;
using Home.Controls.Dialogs;
using Home.Data;
using Home.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Home.Model.Device;

namespace Home
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private static readonly string CACHE_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
        private readonly Client client = new Client() { IsRealClient = true };
        private readonly Home.Communication.API api = null;

        private readonly DispatcherTimer updateTimer = new DispatcherTimer();
        private bool isUpdating = false;
        private readonly object _lock = new object();
        private List<Device> deviceList = new List<Device>();
        private readonly List<DeviceItem> deviceItems = new List<DeviceItem>(); // gui
        private Device lastSelectedDevice = null;
        private bool ignoreSelectionChanged = false;

        public MainWindow()
        {
            InitializeComponent();
            api = new Communication.API("http://192.168.178.38:83");
            client.ID = ClientData.Instance.ClientID;

            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await api.LogoffAsync(client);
        }

        private async void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Initalize();
        }

        public async Task Initalize()
        {
            var result = await api.LoginAsync(client);
            if (result.Result != null)
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
            ignoreSelectionChanged = true;
            DeviceHolder.Items.Clear();
            deviceItems.Clear();

            foreach (var device in deviceList.OrderBy(p => p.Status))
            {
                DeviceItem di = new DeviceItem() { DataContext = device };
                deviceItems.Add(di);
                DeviceHolder.Items.Add(di);
            }

            if (lastSelectedDevice != null)
            {
                int index = deviceList.IndexOf(lastSelectedDevice);
                if (index != -1)
                    DeviceHolder.SelectedIndex = index;
            }

            RefreshSelection();
            ignoreSelectionChanged = false;
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

        private async Task GetScreenshot(Device device, string fileName = "")
        {
            byte[] data = null;
            bool saveInCache = false;
            bool updateGui = (lastSelectedDevice.ID == device.ID);

            string cacheDevicePath = System.IO.Path.Combine(CACHE_PATH, device.ID);
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
                    var fiName = device.ScreenshotFileNames.LastOrDefault();
                    if (fiName != null)
                    {
                        var lastScrenshot = await api.RecieveScreenshotAsync(device, fiName);
                        if (lastScrenshot.Success)
                        {
                            data = Convert.FromBase64String(lastScrenshot.Result.Data);
                            saveInCache = true;
                            fileName = fiName;

                            // Last refresh = now
                            if (updateGui && DateTime.TryParseExact(fileName, Consts.SCREENSHOT_DATE_FILE_FORMAT, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out DateTime result))
                                TextLastScreenshotRefresh.Text = $"{result.ToShortDateString()} @ {result.ToShortTimeString()}";
                            else
                                TextLastScreenshotRefresh.Text = "Nie";
                        }
                    }
                    else
                        TextLastScreenshotRefresh.Text = "Nie";
                }
          
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
            if (ignoreSelectionChanged)
                return;

            if (DeviceHolder.SelectedItem is DeviceItem dev)
            {
                lastSelectedDevice = dev.DataContext as Device;
                await RefreshSelectedItem();
                RefreshSelection();
                await GetScreenshot(lastSelectedDevice);

                DeviceInfo.Visibility = Visibility.Visible;
                DeviceInfoHint.Visibility = Visibility.Collapsed;
            }
            else
            {
                lastSelectedDevice = null;
                DeviceInfo.Visibility = Visibility.Collapsed;
                DeviceInfoHint.Visibility = Visibility.Visible;
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
            
            // Generate log entries FlowDocument
            FlowDocument flowDocument = new FlowDocument { FontFamily = new FontFamily("Consolas") };
            Paragraph currentParagraph = new Paragraph();

            foreach (var entry in lastSelectedDevice.LogEntries)
            {
       

                // Get image
                BitmapImage bi = new BitmapImage { CacheOption = BitmapCacheOption.OnLoad };
                bi.BeginInit();
                string resourceName = string.Empty;
                SolidColorBrush foregroundBrush = null;


                switch (entry.Level)
                {
                    case LogEntry.LogLevel.Debug:
                    case LogEntry.LogLevel.Information:
                        {
                            resourceName = "info.png";
                            foregroundBrush = new SolidColorBrush(Colors.Gray);
                        }
                        break;
                    case LogEntry.LogLevel.Warning:
                        {
                            resourceName = "warning.png";
                            foregroundBrush = new SolidColorBrush(Colors.Orange);
                        }
                        break;
                    case LogEntry.LogLevel.Error:
                        {
                            resourceName = "error.png";
                            foregroundBrush = new SolidColorBrush(Colors.Red);
                        }
                        break;
                }
                
                bi.UriSource = new Uri($"pack://application:,,,/Home;Component/resources/icons/{resourceName}");
                bi.EndInit();

                currentParagraph.Inlines.Add(new InlineUIContainer(new Image() { Source = bi, Width = 20, Margin = new Thickness(0,2,2,0) }) { BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new Run($"[{entry.Timestamp.ToShortDateString()} {entry.Timestamp.ToShortTimeString()}]: ") { Foreground = new SolidColorBrush(Colors.Green), BaselineAlignment = BaselineAlignment.TextTop  });
                currentParagraph.Inlines.Add(new Run(entry.Message) { Foreground = foregroundBrush, BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new LineBreak());
         
            }


            flowDocument.Blocks.Add(currentParagraph);

            // ToDo: Don't create a new viewer each time (Fixed viewer on TabLog)
            var viewer = new FlowDocumentScrollViewer { Document = flowDocument };
            TabLog.Content = viewer;

            TextDeviceLog.ScrollToEnd();
            DeviceInfo.DataContext = null;
            DeviceInfo.DataContext = lastSelectedDevice;
        }

        private async void HyperLinkRefreshScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await api.AquireScreenshotAsync(client, lastSelectedDevice);

        }

        private void ImageScreenshot_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {               
                new ScreenshotDialog(ImageScreenshot.Source).ShowDialog();
            }
        }

        private async void MenuButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await api.ClearDeviceLogAsync(lastSelectedDevice);
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

                if (string.IsNullOrEmpty(image))
                    image = "hdd"; // defaults to hdd

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

    public class OSNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OSType type)
            {
                string result = string.Empty;
                switch (type)
                {
                    case OSType.Linux: result = "Linux"; break;
                    case OSType.LinuxMint: result = "Linux Mint"; break;
                    case OSType.LinuxUbuntu: result = "Ubuntu"; break;
                    case OSType.WindowsXP: result = "Windows XP"; break;
                    case OSType.WindowsaVista: result = "Windows Vista"; break;
                    case OSType.Windows7: result = "Windows 7"; break;
                    case OSType.Windows8: result = "Windows 8"; break;
                    case OSType.Windows10: result = "Windows 10"; break;
                    case OSType.Unix: result = "Unix"; break;
                    case OSType.Other: result = "Anderes OS"; break;
                }


                return result;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value?.ToString()))
                return "Unbekannt";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
