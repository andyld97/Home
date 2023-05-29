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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Home.Model.DeviceChangeEntry;
using Home.Data.Events;
using static SkiaSharp.HarfBuzz.SKShaper;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using System.Diagnostics;
using System.Windows.Interop;
using System.ComponentModel;

namespace Home
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
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

        private static Fluent.IRibbonControl[] deviceDependendButtons;

        static MainWindow()
        {
            try
            {
                System.IO.Directory.CreateDirectory(HomeConsts.CACHE_PATH);
            }
            catch
            { }

            try
            {
                System.IO.Directory.CreateDirectory(HomeConsts.WEBVIEW_CACHE_PATH);
            }
            catch
            { }
        }

        public IEnumerable<Device> GetDevices()
        {
            foreach (var device in deviceList)
                yield return device;
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

            deviceDependendButtons = new Fluent.IRibbonControl[]
            {
                MenuButtonSendMessage,
                MenuButtonShutdownMenu,
                MenuButtonSendCommand,
                MenuButtonDeleteDevice,
                MenuButtonGenerateReport,
                MenuPrintReport,
                MenuButtonClearLog
            };

            // Disable all device depended buttons since there is no device selected at the beginning
            foreach (var element in deviceDependendButtons)
                (element as UIElement).IsEnabled = false;

            AddProtocolEntry(string.Format(Home.Properties.Resources.strStartMessage, typeof(MainWindow).Assembly.GetName().Version.ToString(3)));
            Legend.DataContext = this;
        }

        private async Task CleanUpCacheAsync()
        {
            var di = new DirectoryInfo(HomeConsts.CACHE_PATH);
            var now = DateTime.Now;

            List<FileInfo> files = new List<FileInfo>();

            long length = 0;

            foreach (var file in di.EnumerateFiles("*.png", SearchOption.AllDirectories))
            {
                // Ignore this file if it's parent directory has only one file to keep old screenshots when there is only one available
                if (file.Directory?.EnumerateFiles("*.png", SearchOption.TopDirectoryOnly).Count() == 1)
                    continue;

                // Parse filename as date
                if (DateTime.TryParseExact(System.IO.Path.GetFileNameWithoutExtension(file.Name), Consts.SCREENSHOT_DATE_FILE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    if (date.AddDays(14) > now)
                        files.Add(file);
                }
            }

            length = files.Sum(f => f.Length);

   
            foreach (var file in files) 
            {
                try
                {
                    System.IO.File.Delete(file.FullName);   
                }
                catch
                {
                    // ignore
                }
            }

            if (files.Count > 0)
            {
                var bytes = ByteUnit.FindUnit(length);
                AddProtocolEntry(string.Format(Properties.Resources.strCleanUpResultMessage, files.Count, bytes));
            }
        }

        #region Protocol

        private Paragraph cuurrentParagraph;

        /// <summary>
        /// Adds a protocol entry (protocol is only visible on the start page of Home.WPF)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isError"></param>
        /// <param name="isSucess"></param>
        private void AddProtocolEntry(string message, bool isError = false, bool isSucess = false, bool isWarning = false)
        {
            var now = DateTime.Now;
            string formattedMessage = string.Empty;

            if (cuurrentParagraph == null)
            {
                cuurrentParagraph = new Paragraph();
                TextProtocol.Document.Blocks.Clear();
                TextProtocol.Document.Blocks.Add(cuurrentParagraph);
            }

            string level = "Info";
            if (isError)
                level = Properties.Resources.strSendMessageDialog_LogLevel_Error;
            else if (isWarning)
                level = Properties.Resources.strSendMessageDialog_LogLevel_Warning;

            formattedMessage = $"[{level} @ {now.ToString(Properties.Resources.strDateTimeFormat)}]: {message}";
            var run = new Run(formattedMessage);
            if (isError)
                run.Foreground = new SolidColorBrush(Colors.Red);
            else if (isSucess)
                run.Foreground = new SolidColorBrush(Colors.LightGreen);
            else if (isWarning)
                run.Foreground = new SolidColorBrush(Colors.DarkOrange);

            cuurrentParagraph.Inlines.Add(run);
            cuurrentParagraph.Inlines.Add(new LineBreak());
        }
        #endregion

        private async void App_OnShutdownOrRestart(Device device, bool shutdown, bool wol)
        {
            await ShutdownOrRestartAsync(device, shutdown, wol);
        }

        private async void ScreenshotViewer_OnScreenShotAquired(object sender, EventArgs e)
        {
            if (currentDevice == null)
                return;

            var result = await API.AquireScreenshotAsync(CLIENT, currentDevice);
            if (!result.Success)
                MessageBox.Show(result.ErrorMessage, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
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

            try
            {
                webView2Environment = await CoreWebView2Environment.CreateAsync(userDataFolder: HomeConsts.WEBVIEW_CACHE_PATH);
                await webViewReport.EnsureCoreWebView2Async(webView2Environment);

                if (ClientData.Instance.IgnoreWebVie2Error)
                {
                    ClientData.Instance.IgnoreWebVie2Error = false;
                    ClientData.Instance.Save();
                }
            }
            catch (Exception)
            {
                if (!ClientData.Instance.IgnoreWebVie2Error)
                    MessageBox.Show(this, Properties.Resources.strWebView2RuntimeNotFound_Message, Home.Properties.Resources.strWebView2RuntimeNotFound_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    AddProtocolEntry(Properties.Resources.strWebView2RuntimeNotFound_Message, isWarning: true);

                ClientData.Instance.IgnoreWebVie2Error = true;
                ClientData.Instance.Save();
            }
        }

        private async void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CleanUpCacheAsync();
            await Initalize();
        }

        public async Task Initalize()
        {
            var result = await API.LoginAsync(CLIENT);
            if (result.Result != null)
                deviceList = result.Result;

            if (result.Success)
            {
                AddProtocolEntry(string.Format(Properties.Resources.strConnectedSuccessfullyMessage, Settings.Instance.Host), isSucess: true);
                RefreshDeviceHolder();
            }
            else
                AddProtocolEntry(string.Format(Properties.Resources.strFailedToConnectMessage, Settings.Instance.Host, result.ErrorMessage), true);

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

        private async Task ShutdownOrRestartAsync(Device d, bool shutdown, bool wol)
        {
            if (d == null)
                return;

            if (d.OS.IsAndroid())
            {
                MessageBox.Show(Home.Properties.Resources.strAndroidDeviceNoShutdownSupport, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!wol)
            {
                if (shutdown)
                {
                    if (MessageBox.Show(string.Format(Home.Properties.Resources.strDoYouReallyWantToShutdownDevice, d.Name), Home.Properties.Resources.strReally, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        return;
                }
                else
                {
                    if (MessageBox.Show(string.Format(Home.Properties.Resources.strDoYouReallyWantToRestartDevice, d.Name), Home.Properties.Resources.strReally, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        return;
                }

                await API.ShutdownOrRestartDeviceAsync(shutdown, d);
            }
            else
            {
                if (string.IsNullOrEmpty(d.MacAddress))
                {
                    MessageBox.Show(Home.Properties.Resources.strWOLNotPossible, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBox.Show(string.Format(Home.Properties.Resources.strDoYouReallyWantToWakeUpDevice, d.Name), Home.Properties.Resources.strReally, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;           

                var result = await API.WakeOnLanAsync(d);
                if (result.Success)
                    MessageBox.Show(Home.Properties.Resources.strWOL_SuccessfullySentPackage, Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(string.Format(Home.Properties.Resources.strWOL_MagickPackageSendError, result.ErrorMessage), Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        public async Task<Answer<bool>> WakeUpDeviceAsync(string macAddress)
        {
            return await API.WakeOnLanAsync(macAddress);
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
                // Summarize * and string.Empty to a single group
                if (string.IsNullOrEmpty(group.Key) || group.Key.Trim() == "*")
                {
                    foreach (var device in group.OrderBy(d => d.Name))
                        notAssociatedDevices.Add(device);
                    continue;
                }

                DeviceItemGroup deviceItemGroup = new DeviceItemGroup { GroupName = group.Key, RenderMode = DetermineOverviewMode() };
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
                    RenderMode = DetermineOverviewMode()
                };

                PanelOverview.Children.Add(dig);
            }
        }

        #region Update / Process Events

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
                foreach (var item in result.Result)
                    await ProcessEventAsync(item);
            }

            lock (_lock)
            {
                isUpdating = false;
            }
        }

        private async Task ProcessEventAsync(EventQueueItem @event)
        {
            if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.DeviceScreenshotRecieved)
            {
                // Update screenshot viewer
                if (currentDevice != null && currentDevice.ID == @event.DeviceID)
                {
                    currentDevice.Update(@event.EventData.EventDevice, @event.EventData.EventDevice.LastSeen, @event.EventData.EventDevice.Status, true);
                    await ScreenshotViewer.UpdateScreenshotAsync(currentDevice);
                    await RefreshSelectedItem();
                    RefreshDeviceHolder();
                }
                else
                {
                    // Refresh other device an try to download the screenshot in advance
                    var otherDevice = deviceList.FirstOrDefault(d => d.ID == @event.DeviceID);
                    if (otherDevice != null)
                    {
                        otherDevice.Update(@event.EventData.EventDevice, @event.EventData.EventDevice.LastSeen, @event.EventData.EventDevice.Status, true);
                        foreach (var shot in otherDevice.Screenshots)
                            ScreenshotViewer.QueueScreenshotDownload(otherDevice, shot);
                    }
                }
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

                            oldDevice.Update(@event.EventData.EventDevice, @event.EventData.EventDevice.LastSeen, @event.EventData.EventDevice.Status, true);

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
                        else if (@event.EventDescription == EventQueueItem.EventKind.DeviceDeleted)
                        {
                            var device = deviceList.FirstOrDefault(d => d.ID == @event.DeviceID);
                            if (device != null)
                            {
                                deviceList.Remove(device);
                                RefreshDeviceHolder();
                            }
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
                            // ToDo: *** Only receive screenshot (probably await GetScreenshot(oldDevice))

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
                else if (@event.EventDescription == Data.Events.EventQueueItem.EventKind.NewDeviceConnected)
                {
                    deviceList.Add(@event.EventData.EventDevice);
                    RefreshDeviceHolder();
                }
            }
        }


        #endregion

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

                foreach (var element in deviceDependendButtons)
                    (element as UIElement).IsEnabled = true;

                MenuButtonSendMessage.IsEnabled = true;
                scrollToEnd = true;

                if (webView2Environment != null)
                {
                    await webViewReport?.EnsureCoreWebView2Async(webView2Environment);

                    webViewReport.NavigateToString(Report.GenerateHtmlDeviceReport(currentDevice, Properties.Resources.strDateTimeFormat));
                }
            }
            else
            {
                currentDevice = null;
                MenuButtonSendMessage.IsEnabled = false;
                DeviceInfo.Visibility = Visibility.Collapsed;
                DeviceInfoHint.Visibility = Visibility.Visible;

                foreach (var element in deviceDependendButtons)
                    (element as UIElement).IsEnabled = false;
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

            foreach (var entry in currentDevice.LogEntries.OrderBy(e => e.Timestamp))
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
            DeviceHardwareProtocol.Update(currentDevice);

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
            await ShutdownOrRestartAsync(currentDevice, true, false);
        }

        private async void MenuButtonReboot_Click(object sender, RoutedEventArgs e)
        {
            await ShutdownOrRestartAsync(currentDevice, false, false);
        }

        private async void MenuButtonWOL_Click(object sender, RoutedEventArgs e)
        {
            await ShutdownOrRestartAsync(currentDevice, false, true);
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

        private void MenuButtonExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void MenuButtonOpenSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsDialog(true).ShowDialog();

            // This is necessary due to possible theming changes (that everything applies)
            RefreshOverview();
            RefreshDeviceHolder();
            await RefreshSelectedItem();
        }

        public void UpdateGlowingBrush()
        {
            if (Settings.Instance.ActivateGlowingBrush)
                GlowColor = (System.Windows.Media.Color)FindResource("Fluent.Ribbon.Colors.Accent60");
            else
                GlowColor = null;

            NonActiveBorderBrush = new SolidColorBrush(GlowColor.Value);
        }

        private void MenuButtonOpenAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutDialog().ShowDialog();
        }

        #region Report

        private async void MenuButtonGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice == null)
                return;

            var report = Report.GenerateHtmlDeviceReport(currentDevice, Properties.Resources.strDateTimeFormat);

            SaveFileDialog sfd = new SaveFileDialog() { Filter = Home.Properties.Resources.strHtmlReportFilter };
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

        private async void MenuPrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevice == null)
                return;

            int oldTabIndex = TabDevice.SelectedIndex;
            TabDevice.SelectedIndex = TabDevice.Items.Count - 2;
            await Task.Delay(500);

            SaveFileDialog sfd = new SaveFileDialog() { Filter = Home.Properties.Resources.strHtmlReportFilterPDF };
            sfd.FileName = $"{currentDevice.Name}.pdf";
            var result = sfd.ShowDialog();

            // Note we can only print if the view is already rendered!
            if (result.HasValue && result.Value && webView2Environment != null)
                await webViewReport.CoreWebView2.PrintToPdfAsync(sfd.FileName);

            // Restore old tab index
            TabDevice.SelectedIndex = oldTabIndex;
        }
        #endregion

        #region Wake On LAN (WOL)
        private async void MenuButtonWakeOnLan_Click(object sender, RoutedEventArgs e)
        {
            var result = await API.GetSchedulingRulesAsync();
            if (result.Success)
                new ManageDeviceSchedule(result.Result).ShowDialog();
            else
                MessageBox.Show(string.Format(Home.Properties.Resources.strDeviceScheduling_Settings_FailedToRecieveData, result.ErrorMessage), Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void MenuButtonWakeUp_Click(object sender, RoutedEventArgs e)
        {
            new WOLDialog().ShowDialog();
        }
        #endregion

        #region Overview
        private bool ignoreCheckedChangedToggle = false;
        private bool legendDisplayCPU = true;
        private bool legendDisplayRAM = true;
        private bool legendDisplayDISK = true;
        private bool legendDisplayBattery = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool LegendDisplayCPU
        {
            get => legendDisplayCPU;
            set
            {
                if (legendDisplayCPU != value)
                {
                    legendDisplayCPU = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LegendDisplayCPU)));
                }
            }
        }

        public bool LegendDisplayRAM
        {
            get => legendDisplayRAM;
            set
            {
                if (legendDisplayRAM != value)
                {
                    legendDisplayRAM = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LegendDisplayRAM)));
                }
            }
        }

        public bool LegendDisplayDISK
        {
            get => legendDisplayDISK;
            set
            {
                if (legendDisplayDISK != value)
                {
                    legendDisplayDISK = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LegendDisplayDISK)));
                }
            }
        }

        public bool LegendDisplayBattery
        {
            get => legendDisplayBattery;
            set
            {
                if (legendDisplayBattery != value)
                {
                    legendDisplayBattery = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LegendDisplayBattery)));
                }
            }
        }

        private void MenuButtonTotalOverviewShowScreenshots_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckedChangedToggle) return;
            if (MenuButtonTotalOverviewShowScreenshots.IsChecked == true)
            {
                ignoreCheckedChangedToggle = true;
                MenuButtonTotalOverviewShowPlot.IsChecked = false;
                ignoreCheckedChangedToggle = false;
            }

            foreach (var item in PanelOverview.Children.OfType<DeviceItemGroup>())
                item.RenderMode = DetermineOverviewMode();
        }

        private void MenuButtonTotalOverviewShowPlot_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreCheckedChangedToggle) return;
            if (MenuButtonTotalOverviewShowPlot.IsChecked == true)
            {
                ignoreCheckedChangedToggle = true;
                MenuButtonTotalOverviewShowScreenshots.IsChecked = false;
                ignoreCheckedChangedToggle = false;
            }

            foreach (var item in PanelOverview.Children.OfType<DeviceItemGroup>())
                item.RenderMode = DetermineOverviewMode();
        }

        private Mode DetermineOverviewMode()
        {
            Mode result;
            if (MenuButtonTotalOverviewShowScreenshots.IsChecked.Value)
                result = Mode.Screenshot;
            else if (MenuButtonTotalOverviewShowPlot.IsChecked.Value)
                result = Mode.Diagram;
            else result = Mode.Info;

            // Hide legend when info or screenshot is shown
            if (result == Mode.Diagram)
                Legend.Visibility = Visibility.Visible;
            else
                Legend.Visibility = Visibility.Collapsed;

            return result;
        }

        private void MenuButtonToggleOverivew_Checked(object sender, RoutedEventArgs e)
        {
            if (MenuButtonToggleOverivew.IsChecked.Value)
            {
                GridNetworkOverview.Visibility = Visibility.Visible;
                GridIndividualOverview.Visibility = Visibility.Hidden;
                MenuButtonTotalOverviewShowScreenshots.IsEnabled = 
                MenuButtonTotalOverviewShowPlot.IsEnabled = true;
            }
            else
            {
                GridIndividualOverview.Visibility = Visibility.Visible;
                GridNetworkOverview.Visibility = Visibility.Hidden;
                MenuButtonTotalOverviewShowScreenshots.IsEnabled =
                MenuButtonTotalOverviewShowPlot.IsEnabled = false;
                MenuButtonTotalOverviewShowScreenshots.IsChecked =
                MenuButtonTotalOverviewShowPlot.IsChecked = false;
            }
        }

        #endregion
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
                    // Maybe usb disk or mounted image
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

            return Properties.Resources.strNA;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}