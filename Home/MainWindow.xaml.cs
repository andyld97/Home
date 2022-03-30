using ControlzEx.Theming;
using Fluent;
using Home.Controls;
using Home.Controls.Dialogs;
using Home.Data;
using Home.Helper;
using Home.Model;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
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
        /// <summary>
        /// ToDo: *** Move to a Consts.cs file
        /// </summary>
        public static readonly string CACHE_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
        public static Client CLIENT = new Client() { IsRealClient = true };
        public static Communication.API API = null;

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
            API = new Communication.API("http://192.168.178.38:83");
            CLIENT.ID = ClientData.Instance.ClientID;

            Closing += MainWindow_Closing;
            ScreenshotViewer.OnResize += ScreenshotViewer_OnResize;
            ScreenshotViewer.OnScreenShotAquired += ScreenshotViewer_OnScreenShotAquired;

            InitalizeDeviceActivityPlot();
        }    

        private async void ScreenshotViewer_OnScreenShotAquired(object sender, EventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await API.AquireScreenshotAsync(CLIENT, lastSelectedDevice);
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
            await API.LogoffAsync(CLIENT);
        }

        private async void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Initalize();
        }

        public async Task Initalize()
        {
            var result = await API.LoginAsync(CLIENT);
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

        private int oldDeviceCount = -1;

        private void RefreshOverview()
        {
            int onlineDevices = deviceList.Sum(p => p.Status != DeviceStatus.Offline ? 1 : 0);

            // Prevent refreshing when nothing was changed
            if (oldDeviceCount != -1 && onlineDevices == oldDeviceCount)
                return;
            oldDeviceCount = onlineDevices;

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

                DeviceItemGroup deviceItemGroup = new DeviceItemGroup { GroupName = group.Key, IsScreenshotView = ChkOverviewShowScreenshots.IsChecked.Value };
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
                    Devices = notAssociatedDevices,
                    IsScreenshotView = ChkOverviewShowScreenshots.IsChecked.Value
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
            var result = await API.UpdateAsync(CLIENT);
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
                        MessageBox.Show("New device added!", "New device!", MessageBoxButton.OK, MessageBoxImage.Information);

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
            bool updateGui = (lastSelectedDevice?.ID == device.ID);

            await API.DownloadScreenshotToCache(device, CACHE_PATH, fileName);

            string screenShotFileName = fileName;
            if (string.IsNullOrEmpty(fileName))
                screenShotFileName = device.ScreenshotFileNames.LastOrDefault();

            string path = System.IO.Path.Combine(CACHE_PATH, device.ID, screenShotFileName + ".png");

            if (System.IO.File.Exists(path))
            {
                if (updateGui && DateTime.TryParseExact(screenShotFileName, Consts.SCREENSHOT_DATE_FILE_FORMAT, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out DateTime result))
                    ScreenshotViewer.UpdateDate($"{result.ToShortDateString()} @ {result.ToShortTimeString()}");
                else if (updateGui)
                    ScreenshotViewer.UpdateDate(null);

                try
                {
                    data = System.IO.File.ReadAllBytes(path);
                }
                catch
                {
                    // ignore
                }
            }
            else if (updateGui)
                ScreenshotViewer.UpdateDate(null);


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

                    if (data != null)
                        bi = ImageHelper.LoadImage(ms);
                    else
                        bi = ImageHelper.LoadImage(string.Empty, true);

                    if (device.Status == Device.DeviceStatus.Offline)
                        ScreenshotViewer.SetImageSource(ImageHelper.GrayscaleBitmap(bi));
                    else
                        ScreenshotViewer.SetImageSource(bi);
                }
                catch (Exception)
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

                bi = ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/{resourceName}", false);

                currentParagraph.Inlines.Add(new InlineUIContainer(new Image() { Source = bi, Width = 20, Margin = new Thickness(0, 2, 2, 0) }) { BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new Run($"[{entry.Timestamp.ToShortDateString()} {entry.Timestamp.ToShortTimeString()}]: ") { Foreground = new SolidColorBrush(Colors.Green), BaselineAlignment = BaselineAlignment.TextTop });
                currentParagraph.Inlines.Add(new Run(entry.Message) { Foreground = foregroundBrush, BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new LineBreak());
            }


            flowDocument.Blocks.Add(currentParagraph);
            LogHolder.Document = flowDocument;
            LogScrollViewer.ScrollToEnd();

            RenderPlot();

            DeviceInfo.DataContext = null;
            DeviceInfo.DataContext = lastSelectedDevice;
            ScreenshotViewer.UpdateDevice(lastSelectedDevice);
        }

        #region Activity Plot

        private void RenderPlot()
        {
            List<Point> cpuPoints = new List<Point>();
            List<Point> ramPoints = new List<Point>();
            List<Point> diskPoints = new List<Point>();

            int cpuCounter = 1;
            foreach (var cpu in lastSelectedDevice.Usage.CPU)
                cpuPoints.Add(new Point(cpuCounter++, cpu));

            int ramCounter = 1;
            foreach (var ram in lastSelectedDevice.Usage.RAM)
                ramPoints.Add(new Point(ramCounter++, Math.Round((ram / lastSelectedDevice.Envoirnment.TotalRAM) * 100, 2)));

            int diskCounter = 1;
            foreach (var disk in lastSelectedDevice.Usage.DISK)
                diskPoints.Add(new Point(diskCounter++, disk));

            //  Fill = new SolidColorPaint(SkiaSharp.SKColors.LightBlue.WithAlpha(128)),

            void mapping(Point s, ChartPoint e)
            {
                e.SecondaryValue = s.X; 
                e.PrimaryValue = s.Y;
            }

            var cpuSeries = new LineSeries<Point>()
            {
                Values = cpuPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.AliceBlue, 3),
                Fill = null,
                GeometrySize = 0,
                Name = "CPU Usage (%)",
                TooltipLabelFormatter = (s) => $"CPU: {s.PrimaryValue} %",
            };

            var ramSeries = new LineSeries<Point>()
            {
                Values = ramPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.Violet, 3),
                Fill = null,
                GeometrySize = 0,
                Name = "RAM Usage (%)",
                TooltipLabelFormatter = (s) => $"RAM: {s.PrimaryValue} %",
            };

            var diskSeries = new LineSeries<Point>()
            {
                Values = diskPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.Orange, 3),
                Fill = null,
                GeometrySize = 0,
                Name = "DISK Usage (%)",
                TooltipLabelFormatter = (s) => $"DISK: {s.PrimaryValue} %",
            };

            DeviceActivityPlot.Series = new ISeries[] { cpuSeries, ramSeries, diskSeries };
        }

        private void InitalizeDeviceActivityPlot()
        {
            // This legened is always switching colors, so I am going to use my own legend!
            DeviceActivityPlot.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden; // top
            DeviceActivityPlot.LegendOrientation = LiveChartsCore.Measure.LegendOrientation.Horizontal;
            DeviceActivityPlot.LegendBackground = FindResource("WhiteBrush") as SolidColorBrush;
            DeviceActivityPlot.LegendTextBrush = FindResource("BlackBrush") as SolidColorBrush;

            var xaxis = DeviceActivityPlot.XAxes.FirstOrDefault();
            var yaxis = DeviceActivityPlot.YAxes.FirstOrDefault();
            yaxis.Labeler = (y) => $"{y}%";
            xaxis.Labeler = (x) => {
                if (lastSelectedDevice == null)
                    return string.Empty;

                var n = lastSelectedDevice.LastSeen.AddMinutes(-(60 - x));
                return n.ToString("HH:mm");
            };
        }

        #endregion

        #region Menu

        private async void MenuButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;

            var result = await API.ClearDeviceLogAsync(lastSelectedDevice);
        }

        private void MenuButtonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice != null)
                new SendMessage(lastSelectedDevice, API).ShowDialog();
        }

        private void MenuButtonSendCommand_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice != null)
                new SendCommandDialog(lastSelectedDevice, API).ShowDialog();
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

            await API.ShutdownOrRestartDeviceAsync(true, lastSelectedDevice);
        }

        private async void MenuButtonReboot_Click(object sender, RoutedEventArgs e)
        {
            if (lastSelectedDevice == null)
                return;
            if (MessageBox.Show($"Sind Sie sich sicher, dass Sie das Gerät {lastSelectedDevice.Name} neustarten möchten?", "Wirklich?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            await API.ShutdownOrRestartDeviceAsync(false, lastSelectedDevice);
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

        private void ChkOverviewShowScreenshots_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in PanelOverview.Children.OfType<DeviceItemGroup>())
                item.IsScreenshotView = ChkOverviewShowScreenshots.IsChecked.Value;
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
                    return ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/media/{image}.png", false);
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
