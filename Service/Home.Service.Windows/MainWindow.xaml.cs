using Home.Data;
using Home.Data.Com;
using Home.Measure.Windows;
using Home.Model;
using Home.Service.Windows.Model;
using Home.Service.Windows.Properties;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using static Home.Model.Device;

namespace Home.Service.Windows
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Home.Communication.API api = null;
        private LegacyAPI legacyAPI = null;

        private readonly DateTime startTimestamp = DateTime.Now;

        private Device currentDevice = null;
        private readonly DispatcherTimer ackTimer = new DispatcherTimer();
        private readonly DispatcherTimer updateTimer = new DispatcherTimer();
        private bool isInitalized = false;
        private bool isSendingAck = false;
        private readonly object _lock = new object();

        private string homeVbsAutostartFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "home.vbs");

        // LEGACY-FLAG
        // The legacy flag is mode for Windows 7 SP1 x86 PCs.
        // It's using a similar implemented API like Home.Service.Legacy,
        // because since Home.Service.Windows stopped working on Windows 7 x86 (since .NET 6) (.NET 4.8 worked fine)

        public MainWindow()
        {
            InitializeComponent();
            api = new Communication.API("http://localhost:5000");

            CmbDeviceType.Items.Clear();
            CmbDeviceType.ItemsSource = Enum.GetValues(typeof(Device.DeviceType));
            CmbDeviceType.SelectedIndex = 0;
            CmbOS.Items.Clear();
            CmbOS.ItemsSource = Enum.GetValues(typeof(Device.OSType));
            CmbOS.SelectedIndex = 0;

            LoadSettings();

            // If it is the first start, set simulate the /config flag
            if (!ServiceData.Instance.HasLoggedInOnce)
                App.IsConfigFlagSet = true;

            // Hide the window if it is not the first start and not the config flag
            if (ServiceData.Instance.HasLoggedInOnce && !App.IsConfigFlagSet)
                Opacity = 0;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.IsConfigFlagSet)
                return;

            if (ServiceData.Instance.HasLoggedInOnce && !App.IsConfigFlagSet)
            {
                // WindowState = WindowState.Minimized;
                Visibility = Visibility.Hidden;
                await InitalizeService();
                isInitalized = true;
            }
        }

        private async Task InitalizeService()
        {
            if (ServiceData.Instance.UpdateOnStartup && !App.IsConfigFlagSet)
            {
                if (await UpdateService.CheckForUpdatesAsync() == true)
                {
                    if (await UpdateService.UpdateServiceClient())
                        return;
                }
            }

            if (App.IsConfigFlagSet)
                App.StartAPIThread();

            api = new Communication.API(ServiceData.Instance.APIUrl);
            legacyAPI = new LegacyAPI(ServiceData.Instance.APIUrl);

            var now = DateTime.Now;
            var address = NET.DetermineIPAddress();
            currentDevice = new Device()
            {
                ID = ServiceData.Instance.ID,
                IP = address.IpAddress,
                MacAddress = address.MacAddress,
                DeviceGroup = ServiceData.Instance.DeviceGroup,
                Location = ServiceData.Instance.Location,
                Name = NET.GetMachineName(),
                OS = ServiceData.Instance.SystemType,
                Type = ServiceData.Instance.Type,
                DiskDrives = new System.Collections.ObjectModel.ObservableCollection<DiskDrive>(JsonConvert.DeserializeObject<List<DiskDrive>>(WMI.DetermineDiskDrives())),
                Environment = new DeviceEnvironment()
                {
                    CPUCount = Environment.ProcessorCount,
                    CPUName = WMI.DetermineCPUName(),
                    TotalRAM = Native.DetermineTotalRAM(),
                    OSName = NET.GetOsFriendlyName(ServiceData.Instance.OSName),
                    MachineName = NET.GetMachineName(),
                    Motherboard = WMI.DetermineMotherboard(),
                    OSVersion = Environment.OSVersion.ToString(),
                    RunningTime = now.Subtract(startTimestamp),
                    StartTimestamp = startTimestamp
                },
                Screens = new System.Collections.ObjectModel.ObservableCollection<Screen>(GetScreenInformation()),
                ServiceClientVersion = $"vWindows{typeof(MainWindow).Assembly.GetName().Version.ToString(3)}"
            };

            // Set BIOS info
            WMI.GetBIOSInfo(out string vendor, out string version, out string description, out DateTime? releaseDate);
            if (!string.IsNullOrEmpty(vendor) || !string.IsNullOrEmpty(version) || !string.IsNullOrEmpty(description) || releaseDate != null)
                currentDevice.BIOS = new BIOS() { ReleaseDate = releaseDate ?? DateTime.MinValue, Vendor = vendor, Description = description, Version = version };

            // Run tick manually on first_start
            if (ServiceData.Instance.HasLoggedInOnce)
                isInitalized = true;

#if LEGACY
            if (!isInitalized)
                isInitalized = legacyAPI.RegisterDeviceAsync(currentDevice);
#else
            if (!isInitalized)
                isInitalized = (await api.RegisterDeviceAsync(currentDevice)).Item1;
#endif

            if (isInitalized)
                await SendAck();

            if (!ServiceData.Instance.HasLoggedInOnce)
                ServiceData.Instance.HasLoggedInOnce = true;

            ackTimer.Tick += AckTimer_Tick;
            ackTimer.Interval = TimeSpan.FromMinutes(1);
            ackTimer.Start();

            if (ServiceData.Instance.UseUpdateTimer)
            {
                updateTimer.Tick += UpdateTimer_Tick;
                updateTimer.Interval = TimeSpan.FromHours(ServiceData.Instance.UpdateTimerIntervalHours);
                updateTimer.Start();
            }
        }

        private static List<Screen> GetScreenInformation()
        {
            List<Screen> screens = new List<Screen>();

            foreach (var screen in WMI.GetScreenInformation())
            {
                screens.Add(new Screen()
                {
                    ID = screen["id"].Value<string>(),
                    Manufacturer = screen["manufacturer"].Value<string>(),
                    Serial = screen["serial"].Value<string>(),
                    BuiltDate = screen["built_date"].Value<string>(),
                    Index = screen["index"].Value<int>(),
                    IsPrimary = screen["is_primary"].Value<bool>(),
                    DeviceName = screen["device_name"].Value<string>(),
                    Resolution = screen["resolution"].Value<string>(),
                });
            }

            return screens;
        }

        #region Timer

        private async void AckTimer_Tick(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (isSendingAck)
                    return;
                else
                    isSendingAck = false;
            }

            if (!isInitalized)
            {
                // Initialize
#if LEGACY
                isInitalized = legacyAPI.RegisterDeviceAsync(currentDevice);
#else
                isInitalized = (await api.RegisterDeviceAsync(currentDevice)).Item1;

                // LOG
#endif
            }
            else
            {
                // Send ack
                await SendAck();
            }

            lock (_lock)
            {
                isSendingAck = false;
            }
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (await UpdateService.CheckForUpdatesAsync() == true)
            {
                // Stop timers
                updateTimer.Stop();
                ackTimer.Stop();

                // Do the "update"
                if (await UpdateService.UpdateServiceClient())
                    return;
            }
        }

        #endregion

        #region ACK

        private async Task SendAck()
        {
            var now = DateTime.Now;
            var address = NET.DetermineIPAddress();

            currentDevice.IP = address.IpAddress;
            currentDevice.MacAddress = address.MacAddress;
            currentDevice.DiskDrives = new System.Collections.ObjectModel.ObservableCollection<DiskDrive>(JsonConvert.DeserializeObject<List<DiskDrive>>(WMI.DetermineDiskDrives()));
            currentDevice.Environment.RunningTime = now.Subtract(startTimestamp);
            currentDevice.Environment.OSVersion = Environment.OSVersion.ToString();
            currentDevice.Environment.CPUCount = Environment.ProcessorCount;
            currentDevice.Environment.TotalRAM = Native.DetermineTotalRAM();
            currentDevice.Environment.FreeRAM = Performance.DetermineFreeRAM();
            currentDevice.Environment.CPUUsage = Performance.GetCPUUsage();
            currentDevice.Environment.DiskUsage = Performance.GetDiskUsage();
            currentDevice.Environment.Is64BitOS = Environment.Is64BitOperatingSystem;
            currentDevice.Environment.MachineName = NET.GetMachineName();
            currentDevice.Environment.UserName = Environment.UserName;
            currentDevice.Environment.DomainName = Environment.UserDomainName;
            currentDevice.Environment.GraphicCards = new System.Collections.ObjectModel.ObservableCollection<string>(WMI.DetermineGraphicsCardNames());
            currentDevice.ServiceClientVersion = $"vWindows{typeof(MainWindow).Assembly.GetName().Version.ToString(3)}";
            WMI.GetVendorInfo(out string product, out string description, out string vendor);
            currentDevice.Environment.Product = product;
            currentDevice.Environment.Description = description;
            currentDevice.Environment.Vendor = vendor;
            currentDevice.Screens = new System.Collections.ObjectModel.ObservableCollection<Screen>(GetScreenInformation());

            bool batteryResult = Home.Measure.Windows.NET.DetermineBatteryInfo(out int batteryPercentage, out bool isCharging);
            if (batteryResult)
                currentDevice.BatteryInfo = new Battery() { BatteryLevelInPercent = batteryPercentage, IsCharging = isCharging };

            // Send ack
            Answer<AckResult> ackResult = null;

#if LEGACY
            ackResult = legacyAPI.SendAckAsync(currentDevice);
#else
            ackResult = await api.SendAckAsync(currentDevice);
#endif

            // Process ack answer
            if (ackResult != null && ackResult.Success && ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.OK))
            {
                if (ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.ScreenshotRequired))
                    await PostScreenshot();

                if (ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.MessageRecieved))
                {
                    // Detect Windows version, if Windows Version >= 10, Toast
                    // otherwise old MessageBox
                    string encodedMessage = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ackResult.Result.JsonData));

                    try
                    {
                        if (System.Environment.OSVersion.Version.Major >= 10)
                        {
                            // Windows 10 or higher
                            System.Diagnostics.Process.Start(@"Toast\HomeNotification.exe", encodedMessage);
                        }
                        else
                        {
                            // old method
                            // Notification is in a different folder, otherwise there are problems with different versions of Newtonsoft.JSON
                            System.Diagnostics.Process.Start(@"Notification\Notification.exe", encodedMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else if (ackResult.Result.Result.HasFlag(AckResult.Ack.CommandRecieved))
                {
                    try
                    {
                        var command = JsonConvert.DeserializeObject<Command>(ackResult.Result.JsonData);
                        if (command != null)
                            System.Diagnostics.Process.Start(command.Executable, command.Parameter);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        public async Task PostScreenshot()
        {
            if (!ServiceData.Instance.PostScreenshots)
                return;

            string fileName = System.IO.Path.Combine(ServiceData.SCREENSHOT_PATH, $"capture{DateTime.Now.ToString(Consts.SCREENSHOT_DATE_FILE_FORMAT)}.png");
            var result = NET.CreateScreenshot(fileName);

#if LEGACY
            var apiResult = legacyAPI.SendScreenshotAsync(new Screenshot() { DeviceID = ServiceData.Instance.ID, Data = Convert.ToBase64String(result) });
#else
            // "full screenshot"
            var apiResult = await api.SendScreenshotAsync(new Screenshot() { DeviceID = ServiceData.Instance.ID, Data = Convert.ToBase64String(result) });

            // Single screenshot for each screen (only if there is more than one screen, otherwise if there is only 1 screen, we would have 2 equal screenshots)
            if (System.Windows.Forms.Screen.AllScreens.Length > 1)
            {
                int screenIndex = 0;
                foreach (var screen in System.Windows.Forms.Screen.AllScreens.OrderBy(p => p.DeviceName))
                {
                    var screenshot = Convert.ToBase64String(NET.CaputreScreen(screen));
                    var tempResult = await api.SendScreenshotAsync(new Screenshot()
                    {
                        DeviceID = ServiceData.Instance.ID,
                        ScreenIndex = screenIndex++,
                        Data = screenshot
                    });
                }
            }
#endif
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (App.IsConfigFlagSet)
                return;

            e.Cancel = ServiceData.Instance.HasLoggedInOnce;
            if (ServiceData.Instance.HasLoggedInOnce)
                WindowState = WindowState.Minimized;
        }

        #region Settings

        private void LoadSettings()
        {
            var data = Model.ServiceData.Instance;
            TextAPIUrl.Text = data.APIUrl;
            TextGroup.Text = data.DeviceGroup;
            TextLocation.Text = data.Location;
            CmbOS.SelectedItem = data.SystemType;
            CmbDeviceType.SelectedItem = data.Type;
            chkEnableScreenshots.IsChecked = data.PostScreenshots;
            chkEnableFileAccess.IsChecked = data.AllowRemoteFileAccess;
            chkEnableUpdatesOnStartup.IsChecked = ServiceData.Instance.UpdateOnStartup;
            chkEnableUpdateSearch.IsChecked = ServiceData.Instance.UseUpdateTimer;
            NumUpdateHours.Value = ServiceData.Instance.UpdateTimerIntervalHours;

            RefreshID();

            if (System.IO.File.Exists(homeVbsAutostartFilePath))
                chkEnableStartupOnBoot.Visibility = Visibility.Collapsed;

            if (App.IsConfigFlagSet)
                ButtonInitalize.Content = Home.Service.Windows.Properties.Resources.strSaveAndClose;
        }

        private bool SaveSettings()
        {
            // Apply settings
            string host = TextAPIUrl.Text;
            OSType os = (OSType)CmbOS.SelectedIndex;
            DeviceType dt = (DeviceType)CmbDeviceType.SelectedIndex;
            string location = TextLocation.Text;
            string deviceGroup = TextGroup.Text;

            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(location) && !string.IsNullOrEmpty(deviceGroup))
            {
                ServiceData.Instance.APIUrl = host;
                ServiceData.Instance.OSName = os.ToString();
                ServiceData.Instance.SystemType = os;
                ServiceData.Instance.Type = dt;
                ServiceData.Instance.Location = location;
                ServiceData.Instance.DeviceGroup = deviceGroup;
                ServiceData.Instance.PostScreenshots = chkEnableScreenshots.IsChecked.Value;
                ServiceData.Instance.UpdateOnStartup = chkEnableUpdatesOnStartup.IsChecked.Value;
                ServiceData.Instance.UseUpdateTimer = chkEnableUpdateSearch.IsChecked.Value;
                ServiceData.Instance.UpdateTimerIntervalHours = NumUpdateHours.Value;
                ServiceData.Instance.AllowRemoteFileAccess = chkEnableFileAccess.IsChecked.Value;

                if (ChkEditId.IsChecked.Value == true)
                {
                    if (ServiceData.Instance.ID != currentDevice.ID)
                    {
                        var id = TextGUID.Text;
                        ServiceData.Instance.ID = id;
                        ServiceData.Instance.HasLoggedInOnce = true;
                        currentDevice.ID = id;
                        isInitalized = true;
                    }
                }

                return true;
            }

            return false;
        }
        #endregion

        #region Setup
        private async Task SetupAutostart()
        {
            if (chkEnableStartupOnBoot.IsChecked == true && !System.IO.File.Exists(homeVbsAutostartFilePath))
            {
                try
                {
                    string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Home.Service.Windows.exe");
                    string fileContent = string.Format(Properties.Resources.strHomeVBSStartupFile, path);

                    await System.IO.File.WriteAllTextAsync(homeVbsAutostartFilePath, fileContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Home.Service.Windows.Properties.Resources.strFailedToSetupAutostart, ex.Message), Home.Service.Windows.Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ButtonInitalize_Click(object sender, RoutedEventArgs e)
        {
            if (SaveSettings())
            {
                await SetupAutostart();
                await InitalizeService();

                // WindowState = WindowState.Minimized;
                Visibility = Visibility.Hidden;
            }
            else
                MessageBox.Show(Home.Service.Windows.Properties.Resources.strInvalidDataSet, Home.Service.Windows.Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion

        private void ChkEditId_Checked(object sender, RoutedEventArgs e)
        {
            if (TextGUID.Text == Properties.Resources.strDeviceNotYetRegistered)
                TextGUID.Clear();

            TextGUID.Background = null;
            TextGUID.IsReadOnly = false;    
        }

        private void ChkEditId_Unchecked(object sender, RoutedEventArgs e)
        {
            TextGUID.IsReadOnly = true;
            TextGUID.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
            RefreshID();
        }

        private void RefreshID()
        {
            var data = Model.ServiceData.Instance;
            if (string.IsNullOrEmpty(data.ID))
                TextGUID.Text = Properties.Resources.strDeviceNotYetRegistered;
            else
                TextGUID.Text = data.ID;
        }
    }

    #region Converter

    public class BooleanToVisibiltyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Visible;

            return Visibility.Collapsed;    
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumDescriptionConverter : IValueConverter
    {
        private string GetEnumDescription(Enum enumObj)
        {
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());
            object[] attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
                return enumObj.ToString();
            else
            {
                DescriptionAttribute attrib = null;

                foreach (var att in attribArray)
                {
                    if (att is DescriptionAttribute)
                        attrib = att as DescriptionAttribute;
                }

                if (attrib != null)
                    return attrib.Description;

                return enumObj.ToString();
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum myEnum = (Enum)value;
            string description = GetEnumDescription(myEnum);
            return description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }


    #endregion

    public class LegacyAPI
    {
        private readonly string url;

        public LegacyAPI(string url)
        {
            this.url = url;
        }

        public bool RegisterDeviceAsync(Device d)
        {
            try
            {
                string url = $"{this.url}/api/v1/device/register";
                var http = (HttpWebRequest)WebRequest.Create(new Uri(url));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(d);
                byte[] bytes = System.Text.Encoding.Default.GetBytes(parsedContent);

                using (System.IO.Stream newStream = http.GetRequestStream())
                {
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();

                    var stream = response.GetResponseStream();
                    var sr = new System.IO.StreamReader(stream);
                    var content = sr.ReadToEnd();

                    var obj = JsonConvert.DeserializeObject<Answer<bool>>(content);
                    if (obj != null && obj.Status == "ok")
                        return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register device: {ex.Message}");
            }

            return false;
        }


        public bool SendScreenshotAsync(Screenshot shot)
        {
            try
            {
                string url = $"{this.url}/api/v1/device/screenshot";
                var http = (HttpWebRequest)WebRequest.Create(new Uri(url));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(shot);
                byte[] bytes = System.Text.Encoding.Default.GetBytes(parsedContent);

                using (System.IO.Stream newStream = http.GetRequestStream())
                {
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();

                    var stream = response.GetResponseStream();
                    var sr = new System.IO.StreamReader(stream);
                    var content = sr.ReadToEnd();

                    var obj = JsonConvert.DeserializeObject<Answer<bool>>(content);
                    if (obj != null && obj.Status == "ok")
                        return true;
                }
            }
            catch (Exception ex)
            {
                // ToDo: Log
                Console.WriteLine($"Failed to send screenshot: {ex.Message}");
            }

            return false;
        }

        public Answer<AckResult> SendAckAsync(Device d)
        {
            try
            {
                Console.WriteLine("Sending ack ...");

                string url = $"{this.url}/api/v1/device/ack";
                var http = (HttpWebRequest)WebRequest.Create(new Uri(url));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(d);
                Console.WriteLine(parsedContent);
                byte[] bytes = System.Text.Encoding.Default.GetBytes(parsedContent);

                using (System.IO.Stream newStream = http.GetRequestStream())
                {
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();

                    var stream = response.GetResponseStream();
                    var sr = new System.IO.StreamReader(stream);
                    var content = sr.ReadToEnd();

                    return JsonConvert.DeserializeObject<Answer<AckResult>>(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send ack: {ex.Message}");
                return AnswerExtensions.Fail<AckResult>(ex.Message);
            }
        }
    }
}