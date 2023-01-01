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
using Controls.Dialogs;
using Microsoft.AspNetCore.Mvc.TagHelpers;

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
        public static readonly string CACHE_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Home", "Cache");
        public static readonly string WEBVIEW_CACHE_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Home", "WebView2Cache");

        public static Client CLIENT = new Client() { IsRealClient = true };
        public static Communication.API API = null;
        public static MainWindow W_INSTANCE = null;

        private readonly DispatcherTimer updateTimer = new DispatcherTimer();
        private CoreWebView2Environment webView2Environment;
        private bool isUpdating = false;
        private readonly object _lock = new object();
        private List<Device> deviceList = new List<Device>();
        private readonly List<DeviceItem> deviceItems = new List<DeviceItem>(); // gui
        private Device currentDevice = null;
        private bool ignoreSelectionChanged = false;
        private bool scrollToEnd = true;
        private int oldDeviceCount = -1;

        static MainWindow()
        {
            try
            {
                System.IO.Directory.CreateDirectory(CACHE_PATH);
            }
            catch
            { }

            try
            {
                System.IO.Directory.CreateDirectory(WEBVIEW_CACHE_PATH);
            }
            catch
            { }
        }

        public MainWindow()
        {
            InitializeComponent();
            W_INSTANCE = this;

            if (string.IsNullOrEmpty(Settings.Instance.Host))
            {
                var result = new SettingsDialog(false).ShowDialog();
                if (result.HasValue && !result.Value)
                    return;
            }

            API = new Communication.API(Settings.Instance.Host);
            CLIENT.ID = ClientData.Instance.ClientID;

            Closing += MainWindow_Closing;
            ScreenshotViewer.OnResize += ScreenshotViewer_OnResize;
            ScreenshotViewer.OnScreenShotAquired += ScreenshotViewer_OnScreenShotAquired;
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

            webView2Environment = await CoreWebView2Environment.CreateAsync(userDataFolder: WEBVIEW_CACHE_PATH);
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
                MessageBox.Show(result.ErrorMessage, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);

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

            foreach (var device in deviceList.OrderBy(p => p.Name).ThenBy(p => p.Status))
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
                MessageBox.Show(Home.Properties.Resources.strAndroidDeviceNoShutdownSupport, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (shutdown)
            {
                if (MessageBox.Show(string.Format(Home.Properties.Resources.strDoYouReallyWantToShutdownDevice, d.Name), "Wirklich?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            }
            else
            {
                if (MessageBox.Show(string.Format(Home.Properties.Resources.strDoYouReallyWantToRestartDevice, d.Name), "Wirklich?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
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
                var @event = result.Result;

                if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.DeviceScreenshotRecieved)
                {
                    // Update screenshot viewer
                    await ScreenshotViewer.UpdateScreenshotAsync(@event.EventData.EventDevice);
                }
                else
                {
                    if (deviceList.Any(d => d.ID == @event.DeviceID))
                    {
                        // Update 
                        var oldDevice = deviceList.Where(d => d.ID == @event.DeviceID).FirstOrDefault();
                        if (oldDevice != null)
                        {
                            if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.ACK)
                            {
                                bool updateScreenshot = false;
                                if (oldDevice.Status == DeviceStatus.Active && @event.EventData.EventDevice.Status == DeviceStatus.Offline)
                                {
                                    // Update grayscale shot
                                    updateScreenshot = true;
                                }

                                // deviceList[deviceList.IndexOf(oldDevice)] = device.EventData.EventDevice;
                                oldDevice.Update(@event.EventData.EventDevice, @event.EventData.EventDevice.LastSeen, @event.EventData.EventDevice.Status, true);

                                if (currentDevice == oldDevice)
                                {
                                    // lastSelectedDevice = device.EventData.EventDevice;
                                    // RefreshSelectedItem();
                                }

                                await RefreshSelectedItem();
                                RefreshDeviceHolder();

                                if (updateScreenshot)
                                    await ScreenshotViewer.UpdateScreenshotAsync(oldDevice);
                            }
                            else if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.LogCleared)
                            {
                                oldDevice.LogEntries.Clear();
                                await RefreshSelectedItem();
                            }
                            else if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.LogEntriesRecieved)
                            {
                                oldDevice.LogEntries.Clear();
                                foreach (var item in @event.EventData.EventDevice.LogEntries)
                                    oldDevice.LogEntries.Add(item);

                                await RefreshSelectedItem();
                            }
                            else if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.LiveModeChanged)
                            {
                                oldDevice.LogEntries.Clear();
                                foreach (var item in @event.EventData.EventDevice.LogEntries)
                                    oldDevice.LogEntries.Add(item);

                                oldDevice.IsLive = @event.EventData.EventDevice.IsLive;
                                await RefreshSelectedItem();
                            }
                            else if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.DeviceScreenshotRecieved)
                            {
                                // ToDo: *** Only recieve screenshot (probably await GetScreenshot(oldDevice)

                            }
                            else if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.DeviceChangedState)
                            {
                                oldDevice.Status = @event.EventData.EventDevice.Status;

                                // Refresh gui
                                await RefreshSelectedItem();
                                RefreshDeviceHolder();
                            }
                        }
                    }
                    else
                    {
                        deviceList.Add(@event.EventData.EventDevice);
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
                await ScreenshotViewer.UpdateScreenshotAsync(currentDevice);

                DeviceInfo.Visibility = Visibility.Visible;
                DeviceInfoHint.Visibility = Visibility.Collapsed;
                MenuButtonSendMessage.IsEnabled = true;
                scrollToEnd = true;

                await webViewReport.EnsureCoreWebView2Async(webView2Environment);
                webViewReport.NavigateToString(Report.GenerateHtmlDeviceReport(currentDevice, Properties.Resources.strDateTimeFormat));
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
            FlowDocument flowDocument = new FlowDocument { FontFamily = new FontFamily("Cascadia Code") };
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

                bi = ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/{resourceName}", false, false);

                currentParagraph.Inlines.Add(new InlineUIContainer(new Image() { Source = bi, Width = 15, Margin = new Thickness(2, 2, 5, 2) }) { BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new Run($"[{entry.Timestamp.ToString(Properties.Resources.strDateTimeFormat)}]: ") { Foreground = new SolidColorBrush(Colors.Gray), BaselineAlignment = BaselineAlignment.TextTop });
                currentParagraph.Inlines.Add(new Run(entry.Message) { Foreground = foregroundBrush, BaselineAlignment = BaselineAlignment.Bottom });
                currentParagraph.Inlines.Add(new LineBreak());
            }

            flowDocument.Blocks.Add(currentParagraph);

            // Remember old scroll position
            double oldScrollPosition = LogScrollViewer.VerticalOffset;
            LogHolder.Document = flowDocument;

            // Restore old scroll position
            if (!scrollToEnd)
                LogScrollViewer.ScrollToVerticalOffset(oldScrollPosition);
            else
            {
                scrollToEnd = false;
                LogScrollViewer.ScrollToEnd();
            }

            DeviceActivityPlot.RenderPlot(currentDevice);

            DeviceInfo.DataContext = null;
            DeviceInfo.DataContext = currentDevice;
            await ScreenshotViewer.UpdateDeviceAsync(currentDevice);
            DeviceInfoDisplay.UpdateDevice(currentDevice);
        }

        #region Menu

        private async void MenuButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice == null)
                return;

            var result = await API.ClearDeviceLogAsync(currentDevice);

            if (result.Success)
                MessageBox.Show(Home.Properties.Resources.strSuccessfullyClearedDeviceLog, Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(string.Format(Home.Properties.Resources.strFailedToClearDeviceLog, result.ErrorMessage), Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (MessageBox.Show(this, string.Format(Home.Properties.Resources.strAreYouSureToDeleteDevice, currentDevice.Name), Home.Properties.Resources.strAreYouSure, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var result = await API.DeleteDeviceAsync(currentDevice);
                    if (result.Success)
                        MessageBox.Show(Home.Properties.Resources.strTheDeviceWasDeletedSuccessfully, Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show(result.ErrorMessage, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
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

            // ToDo: *** Localize
            if (currentDevice.Status == DeviceStatus.Offline)
            {
                MessageBox.Show(Home.Properties.Resources.strDeviceOfflineCannotExecuteCommand, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currentDevice.OS.IsWindowsLegacy() || currentDevice.OS.IsAndroid())
            {
                MessageBox.Show(Home.Properties.Resources.strFeatureIsNotSupportedOnSelectedDevice, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
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
                    if (dd.VolumeName.Contains(','))
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

        private async void MenuButtonGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice == null)
                return;

            var report = Report.GenerateHtmlDeviceReport(currentDevice, Properties.Resources.strDateTimeFormat);

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

        private void MenuButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuButtonOpenSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsDialog(true).ShowDialog();
        }

        public void UpdateGlowingBrush()
        {
            if (Settings.Instance.ActivateGlowingBrush)
                GlowBrush = new SolidColorBrush((System.Windows.Media.Color)FindResource("Fluent.Ribbon.Colors.AccentColor60"));
            else
                GlowBrush = null;

            NonActiveBorderBrush = GlowBrush;
        }

        private void MenuButtonOpenAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutDialog().Show();
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
                    return ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/media/{image}.png", false, false);
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
            string[] newLine = new string[] { System.Environment.NewLine, "\r", "\n", "\r\n" };

            if (value is string str && newLine.Any(n => str.Contains(n)))
                return str.Split(newLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

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
                return $"{b.BatteryLevelInPercent}% ({Home.Properties.Resources.strCharging}: {(b.IsCharging ? Properties.Resources.strYes : Properties.Resources.strNo)})";

            return "n/a";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}