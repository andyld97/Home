using ControlzEx.Theming;
using Fluent;
using Home.Controls;
using Home.Controls.Dialogs;
using Home.Data;
using Home.Helper;
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
        private readonly Communication.API api = null;

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
            ScreenshotViewer.PassApiAndClient(api, client);
            ScreenshotViewer.OnResize += ScreenshotViewer_OnResize;
            ScreenshotViewer.OnScreenShotAquired += ScreenshotViewer_OnScreenShotAquired;
        }

        private async void ScreenshotViewer_OnScreenShotAquired(object sender, EventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await api.AquireScreenshotAsync(client, lastSelectedDevice);
            if (!result.Success)
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ScreenshotViewer_OnResize(bool isLittle)
        {
            if (!isLittle)
            {
                Grid.SetColumn(ScreeshotViewHolder, 0);
                Grid.SetColumnSpan(ScreeshotViewHolder, 2);
                InfoGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                Grid.SetColumn(ScreeshotViewHolder, 1);
                Grid.SetColumnSpan(ScreeshotViewHolder, 1);
                InfoGrid.Visibility = Visibility.Visible;
            }
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
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            updateTimer.Interval = TimeSpan.FromSeconds(5);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void RefreshDeviceHolder()
        {
            ignoreSelectionChanged = true;
            DeviceHolderAll.Items.Clear();
            DeviceHolderOffline.Items.Clear();
            DeviceHolderActive.Items.Clear();
            deviceItems.Clear();

            foreach (var device in deviceList.OrderBy(p => p.Status))
            {
                DeviceItem di = new DeviceItem() { DataContext = device };
                deviceItems.Add(di);
                DeviceHolderAll.Items.Add(di);

                if (device.Status == DeviceStatus.Active)
                {
                    DeviceItem active = new DeviceItem() { DataContext = device };
                    deviceItems.Add(active);
                    DeviceHolderActive.Items.Add(active);
                }

                if (device.Status == DeviceStatus.Offline)
                {
                    DeviceItem off = new DeviceItem() { DataContext = device };
                    deviceItems.Add(off);
                    DeviceHolderOffline.Items.Add(off);
                }
            }

            if (lastSelectedDevice != null)
            {
                int allIndex = deviceList.IndexOf(lastSelectedDevice);
                if (allIndex != -1)
                    DeviceHolderAll.SelectedIndex = allIndex;

                int activeIndex = deviceList.Where(p => p.Status != DeviceStatus.Offline).ToList().IndexOf(lastSelectedDevice);
                if (activeIndex != -1)
                    DeviceHolderActive.SelectedIndex = activeIndex;

                int offIndex = deviceList.Where(p => p.Status == DeviceStatus.Offline).ToList().IndexOf(lastSelectedDevice);
                if (offIndex != -1)
                    DeviceHolderActive.SelectedIndex = offIndex;
            }

            TextAllDevices.Text = $"Alle Geräte: {deviceList.Count}";
            TextActiveDevices.Text = $"Aktive Geräte {deviceList.Where(p => p.Status != DeviceStatus.Offline).Count()}";
            TextOfflineDevices.Text = $"Inaktive Geräte: {deviceList.Where(p => p.Status == DeviceStatus.Offline).Count()}";
            RefreshSelection();
            ignoreSelectionChanged = false;
            RefreshOverview();
        }

        private void RefreshOverview()
        {
            PanelOverview.Children.Clear();

            var groups = from device in deviceList where device.Status != DeviceStatus.Offline group device by device.Location into gr orderby gr.Count() descending select gr;

            List<Device> notAssociatedDevices = new List<Device>();

            foreach (var group in groups)
            {
                // Summarise * and string.Empty to a single group
                if (string.IsNullOrEmpty(group.Key) || group.Key.Trim() == "*")
                {
                    foreach (var device in group.OrderBy(d => d.Name))
                        notAssociatedDevices.Add(device);

                    continue;
                }

                DeviceItemGroup deviceItemGroup = new DeviceItemGroup { GroupName = group.Key };
                deviceItemGroup.OnGroupSelectionChanged += (string grp) =>
                {
                    foreach (var currentGroup in PanelOverview.Children.OfType<DeviceItemGroup>().Where(d => d.GroupName != grp))
                        currentGroup.ClearSelection();
                };

                foreach (var device in group.OrderBy(d => d.Name))
                    deviceItemGroup.Devices.Add(device);

                PanelOverview.Children.Add(deviceItemGroup);
            }

            // Apply not associated device group (if there are any devices)
            if (notAssociatedDevices.Count > 0)
            {
                DeviceItemGroup dig = new DeviceItemGroup
                {
                    GroupName = "Nicht zugeordnet",
                    Devices = notAssociatedDevices
                };

                PanelOverview.Children.Add(dig);
            }
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
                            bool update = false;
                            if (oldDevice.Status == DeviceStatus.Active && device.EventData.EventDevice.Status == DeviceStatus.Offline)
                            {
                                // Update grayscale shot
                                update = true;
                            }

                            // deviceList[deviceList.IndexOf(oldDevice)] = device.EventData.EventDevice;
                            oldDevice.Update(device.EventData.EventDevice, device.EventData.EventDevice.LastSeen, device.EventData.EventDevice.Status, true);

                            if (lastSelectedDevice == oldDevice)
                            {
                                // lastSelectedDevice = device.EventData.EventDevice;
                                //RefreshSelectedItem();
                            }


                            await RefreshSelectedItem();
                            RefreshDeviceHolder();

                            if (update)
                                await GetScreenshot(oldDevice);
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
            bool updateGui = (lastSelectedDevice?.ID == device.ID);

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
                            ScreenshotViewer.UpdateDate($"{fi.LastAccessTime.ToShortDateString()} @ {fi.LastWriteTime.ToShortTimeString()}");
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
                                ScreenshotViewer.UpdateDate($"{result.ToShortDateString()} @ {result.ToShortTimeString()}");
                            else
                                ScreenshotViewer.UpdateDate(null);
                        }
                    }
                    else
                        ScreenshotViewer.UpdateDate(null);
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
                        ScreenshotViewer.UpdateDate($"{now.ToShortDateString()} @ {now.ToShortTimeString()}");
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

                        ScreenshotViewer.SetImageSource(grayBitmap);
                    }
                    else
                        ScreenshotViewer.SetImageSource(bi);
                }
                catch (Exception ex)
                {
                    ScreenshotViewer.SetImageSource(null);
                }
            }
        }

        private async void DeviceHolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ignoreSelectionChanged)
                return;

            if ((sender as ListBox).SelectedItem is DeviceItem dev)
            {
                lastSelectedDevice = dev.DataContext as Device;
                await RefreshSelectedItem();
                RefreshSelection();
                await GetScreenshot(lastSelectedDevice);

                DeviceInfo.Visibility = Visibility.Visible;
                DeviceInfoHint.Visibility = Visibility.Collapsed;
                MenuButtonSendMessage.IsEnabled = true;
            }
            else
            {
                lastSelectedDevice = null;
                MenuButtonSendMessage.IsEnabled = false;
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

            // Generate log entries FlowDocument
            FlowDocument flowDocument = new FlowDocument { FontFamily = new FontFamily("Consolas") };
            Paragraph currentParagraph = new Paragraph();

            foreach (var entry in lastSelectedDevice.LogEntries)
            {
                // Get image
                BitmapImage bi;
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

                bi = ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/{resourceName}");

                currentParagraph.Inlines.Add(new InlineUIContainer(new Image() { Source = bi, Width = 20, Margin = new Thickness(0, 2, 2, 0) }) { BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new Run($"[{entry.Timestamp.ToShortDateString()} {entry.Timestamp.ToShortTimeString()}]: ") { Foreground = new SolidColorBrush(Colors.Green), BaselineAlignment = BaselineAlignment.TextTop });
                currentParagraph.Inlines.Add(new Run(entry.Message) { Foreground = foregroundBrush, BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new LineBreak());
            }


            flowDocument.Blocks.Add(currentParagraph);
            LogHolder.Document = flowDocument;
            LogScrollViewer.ScrollToEnd();

            DeviceInfo.DataContext = null;
            DeviceInfo.DataContext = lastSelectedDevice;
            ScreenshotViewer.UpdateDevice(lastSelectedDevice);
        }

        private async void HyperLinkRefreshScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await api.AquireScreenshotAsync(client, lastSelectedDevice);

        }

        #region Menu

        private async void MenuButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await api.ClearDeviceLogAsync(lastSelectedDevice);
        }

        private void MenuButtonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice != null)
                new SendMessage(lastSelectedDevice, api).ShowDialog();
        }

        private void MenuButtonSendCommand_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice != null)
                new SendCommandDialog(lastSelectedDevice, api).ShowDialog();
        }

        private void LogHolder_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            LogScrollViewer.ScrollToVerticalOffset(LogScrollViewer.VerticalOffset - e.Delta);
        }


        private void MenuButtonChangeTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
            ThemeManager.Current.ChangeTheme(Application.Current, ThemeManager.Current.GetInverseTheme(ThemeManager.Current.DetectTheme()));
            ThemeManager.Current.SyncTheme();
        }

        private async void MenuButtonShutdown_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;
            if (MessageBox.Show($"Sind Sie sich sicher, dass Sie das Gerät {lastSelectedDevice.Name} herunterfahren möchten?", "Wirklich?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            await api.ShutdownOrRestartDeviceAsync(true, lastSelectedDevice);
        }

        private async void MenuButtonReboot_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;
            if (MessageBox.Show($"Sind Sie sich sicher, dass Sie das Gerät {lastSelectedDevice.Name} neustarten möchten?", "Wirklich?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            await api.ShutdownOrRestartDeviceAsync(false, lastSelectedDevice);
        }    

        #endregion

        private void TabExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (BottomTabControl == null)
                return;

            BottomTabControl.Height = 200;
        }

        private void TabExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (BottomTabControl == null)
                return;

            BottomTabControl.Height = 23;
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
                    return ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/media/{image}.png");
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
