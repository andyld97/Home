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
using static Home.Data.Helper.GeneralHelper;
using Microsoft.Web.WebView2.Core;
using Home.Data.Helper;
using Model;
using Microsoft.Win32;

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
        private CoreWebView2Environment webView2Environment;
        private bool isUpdating = false;
        private readonly object _lock = new object();
        private List<Device> deviceList = new List<Device>();
        private readonly List<DeviceItem> deviceItems = new List<DeviceItem>(); // gui
        private Device currentDevice = null;
        private bool ignoreSelectionChanged = false;
        private int oldDeviceCount = -1;

        public MainWindow()
        {
            InitializeComponent();
            API = new Communication.API("http://192.168.178.38:83");
            CLIENT.ID = ClientData.Instance.ClientID;

            Closing += MainWindow_Closing;
            ScreenshotViewer.OnResize += ScreenshotViewer_OnResize;
            ScreenshotViewer.OnScreenShotAquired += ScreenshotViewer_OnScreenShotAquired;

            InitalizeDeviceActivityPlot();
            App.OnShutdownOrRestart += App_OnShutdownOrRestart;
        }

        private async void App_OnShutdownOrRestart(Device device, bool shutdown)
        {
            await ShutdownOrRestartAsync(device, shutdown);
        }

        private async void ScreenshotViewer_OnScreenShotAquired(object sender, EventArgs e)
        {
            if (currentDevice == null)
                return;

            var result = await API.AquireScreenshotAsync(CLIENT, currentDevice);
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

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            webView2Environment = await CoreWebView2Environment.CreateAsync();

            await webViewReport.EnsureCoreWebView2Async(webView2Environment);
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

            if (currentDevice != null)
            {
                int allIndex = deviceList.IndexOf(currentDevice);
                if (allIndex != -1)
                    DeviceHolderAll.SelectedIndex = allIndex;

                int activeIndex = deviceList.Where(p => p.Status != DeviceStatus.Offline).ToList().IndexOf(currentDevice);
                if (activeIndex != -1)
                    DeviceHolderActive.SelectedIndex = activeIndex;

                int offIndex = deviceList.Where(p => p.Status == DeviceStatus.Offline).ToList().IndexOf(currentDevice);
                if (offIndex != -1)
                    DeviceHolderActive.SelectedIndex = offIndex;
            }

            // Update left tab headers
            TextAllDevices.Text = string.Format(Properties.Resources.strAllDevicesTab, deviceList.Count);
            TextActiveDevices.Text = string.Format(Properties.Resources.strActiveDevicesTab, deviceList.Where(p => p.Status != DeviceStatus.Offline).Count());
            TextOfflineDevices.Text = string.Format(Properties.Resources.strOfflineDevicesTab, deviceList.Where(p => p.Status == DeviceStatus.Offline).Count());

            RefreshSelection();
            ignoreSelectionChanged = false;
            RefreshOverview();
        }

        private async Task ShutdownOrRestartAsync(Device d, bool shutdown)
        {
            if (d == null)
                return;

            if (d.OS.IsAndroid())
            {
                MessageBox.Show($"Ein Android Gerät kann nicht heruntergefahren oder neugestartet werden!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (shutdown)
            {
                if (MessageBox.Show($"Sind Sie sich sicher, dass Sie das Gerät {d.Name} herunterfahren möchten?", "Wirklich?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            }
            else
            {
                if (MessageBox.Show($"Sind Sie sich sicher, dass Sie das Gerät {d.Name} neustarten möchten?", "Wirklich?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            }

            await API.ShutdownOrRestartDeviceAsync(shutdown, d);
        }

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
                    GroupName = Properties.Resources.strDeviceLocationNotAssigend,
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

                            if (currentDevice == oldDevice)
                            {
                                // lastSelectedDevice = device.EventData.EventDevice;
                                // RefreshSelectedItem();
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
            bool updateGui = (currentDevice?.ID == device.ID);

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
                currentDevice = dev.DataContext as Device;
                SwitchFileManager(false);
                await RefreshSelectedItem();
                RefreshSelection();
                await GetScreenshot(currentDevice);

                DeviceInfo.Visibility = Visibility.Visible;
                DeviceInfoHint.Visibility = Visibility.Collapsed;
                MenuButtonSendMessage.IsEnabled = true;

                await webViewReport.EnsureCoreWebView2Async(webView2Environment);
                webViewReport.NavigateToString(Report.GenerateHtmlDeviceReport(currentDevice));
            }
            else
            {
                currentDevice = null;
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
                item.SetSelected(currentDevice?.ID == ctx.ID);
            }
        }

        private async Task RefreshSelectedItem()
        {
            if (currentDevice == null)
                return;

            // Generate log entries FlowDocument
            FlowDocument flowDocument = new FlowDocument { FontFamily = new FontFamily("Consolas") };
            Paragraph currentParagraph = new Paragraph();

            foreach (var entry in currentDevice.LogEntries)
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
            DeviceInfo.DataContext = currentDevice;
            ScreenshotViewer.UpdateDevice(currentDevice);
            CmbGraphics.SelectedIndex = 0;
        }

        #region Activity Plot

        private double NormalizeValue(double input)
        {
            // 0.19 => 19%
            if (Math.Round(input, 0) == 0)
                return input * 100;

            // 500% => 50%
            if (input > 100)
                return Math.Round(input / 100, 2);

            return input;
        }

        private void RenderPlot()
        {
            if (currentDevice == null)
                return;

            List<Point> cpuPoints = new List<Point>();
            List<Point> ramPoints = new List<Point>();
            List<Point> diskPoints = new List<Point>();
            List<Point> batteryPoints = new List<Point>();

            int cpuCounter = 1;
            foreach (var cpu in currentDevice.Usage.CPU)
                cpuPoints.Add(new Point(cpuCounter++, NormalizeValue(cpu)));

            int ramCounter = 1;
            foreach (var ram in currentDevice.Usage.RAM)
                ramPoints.Add(new Point(ramCounter++, Math.Round((ram / currentDevice.Environment.TotalRAM) * 100, 2)));

            int diskCounter = 1;
            foreach (var disk in currentDevice.Usage.DISK)
                diskPoints.Add(new Point(diskCounter++, NormalizeValue(disk)));

            if (currentDevice.BatteryInfo != null)
            {
                int batteryCounter = 1;
                foreach (var bPercent in currentDevice.Usage.Battery)
                    batteryPoints.Add(new Point(batteryCounter++, bPercent));
            }

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

            var batterySeries = new LineSeries<Point>()
            {
                Values = batteryPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.Green, 3),
                Fill = null,
                GeometrySize = 0,
                Name = "Battery Remaning (%)",
                TooltipLabelFormatter = (s) => $"Battery Remaning: {s.PrimaryValue} %"
            };

            List<ISeries> series = new List<ISeries>(); // { cpuSeries, ramSeries, diskSeries };

            if (ChkCPULegend.IsChecked.Value)
                series.Add(cpuSeries);

            if (ChkRAMLegend.IsChecked.Value)
                series.Add(ramSeries);

            if (ChkDiskLegend.IsChecked.Value)
                series.Add(diskSeries);

            if (ChkBatteryLegend.IsChecked.Value && currentDevice.BatteryInfo != null)
                series.Add(batterySeries);

            DeviceActivityPlot.Series = series;
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
            xaxis.Labeler = (x) =>
            {
                if (currentDevice == null)
                    return string.Empty;

                if (currentDevice.LastSeen == DateTime.MinValue)
                    return String.Empty;

                // 60 is not true if there are not 60 values in the list
                // and remember that all values (cpu, ram, disk) MUST have the same amount, also if they get cleard (they get all cleard)
                var n = currentDevice.LastSeen.AddMinutes(-(currentDevice.Usage.CPU.Count - x));
                return n.ToString("HH:mm");
            };
        }
        #endregion

        #region Menu

        private async void MenuButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice == null)
                return;

            var result = await API.ClearDeviceLogAsync(currentDevice);
        }

        private void MenuButtonSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice != null)
                new SendMessageDialog(currentDevice, API).ShowDialog();
        }

        private void MenuButtonSendCommand_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice != null)
                new SendCommandDialog(currentDevice, API).ShowDialog();
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
            await ShutdownOrRestartAsync(currentDevice, true);
        }

        private async void MenuButtonReboot_Click(object sender, RoutedEventArgs e)
        {
            await ShutdownOrRestartAsync(currentDevice, false);
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

        private async void MenuButtonDeleteDevice_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice != null)
            {
                if (MessageBox.Show(this, $"Sind Sie sich sicher, dass Sie das Gerät {currentDevice.Name} löschen möchten?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var result = await API.DeleteDeviceAsync(currentDevice);
                    if (result.Success)
                        MessageBox.Show("Erfolg!", "Erfolg!", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show(result.ErrorMessage, "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SwitchFileManager(bool state)
        {
            if (state)
            {
                ListHDD.Visibility = Visibility.Hidden;
                DeviceExplorer.Visibility = Visibility.Visible;
            }
            else
            {
                ListHDD.Visibility = Visibility.Visible;
                DeviceExplorer.Visibility = Visibility.Hidden;
            }
        }

        private async void ListHDD_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (currentDevice == null)
                return;

            if (currentDevice.Status == DeviceStatus.Offline)
            {
                MessageBox.Show("This devices is currently offline! No io operations can be executed!", "Offline-Mode", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currentDevice.OS.IsWindowsLegacy() || currentDevice.OS.IsAndroid())
            {
                MessageBox.Show("This feature is currently only supported on newer Windows systems (Windows 7 SP1 or newer) or on Linux Systems", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ListHDD.SelectedItem is DiskDrive dd)
            {
                // Under Windows the DriveName is the path (e.g. C:, D:)
                SwitchFileManager(true);

                string driveName = string.Empty;
                if (currentDevice.OS.IsWindows(false))
                    driveName = dd.DriveName;
                else if (currentDevice.OS.IsLinux())
                {
                    if (dd.VolumeName.Contains(","))
                        driveName = dd.VolumeName.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    else
                        driveName = dd.VolumeName;
                }

                await DeviceExplorer.PassWebView2Environment(webView2Environment);
                await DeviceExplorer.NavigateAsync(currentDevice, driveName);
            }
        }

        private void DeviceExplorer_OnHomeButtonPressed()
        {
            SwitchFileManager(false);
        }

        private void ChkCPULegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot();
        }

        private void ChkRAMLegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot();
        }

        private void ChkDiskLegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot();
        }

        private void ChkBatteryLegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot();
        }

        private async void MenuButtonGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice == null)
                return;

            var report = Report.GenerateHtmlDeviceReport(currentDevice);

            SaveFileDialog sfd = new SaveFileDialog() { Filter = "HTML Report File (*.html)|*.html" };
            sfd.FileName = $"{currentDevice.Name}.html";
            var result = sfd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    await System.IO.File.WriteAllTextAsync(sfd.FileName, report);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"{Home.Properties.Resources.strFailedToSaveReport}{Environment.NewLine}{ex.Message}", Home.Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                return Properties.Resources.strUnkown;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BatteryInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Battery b)
                return $"{b.BatteryLevelInPercent}% (Charging: {(b.IsCharging ? "Yes" : "No")})";

            return "n/a";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
