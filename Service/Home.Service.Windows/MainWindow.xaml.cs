using Home.Data;
using Home.Data.Com;
using Home.Measure.Windows;
using Home.Model;
using Home.Service.Windows.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Windows;
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
        private bool isInitalized = false;
        private bool isSendingAck = false;
        private readonly object _lock = new object();

        // LEGACY-FLAG
        // The legacy flag is mode for Windows 7 SP1 x86 PCs.
        // It's using a similiar implemented API like Home.Service.Legacy,
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
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ServiceData.Instance.HasLoggedInOnce)
            {
                // WindowState = WindowState.Minimized;
                Visibility = Visibility.Hidden;
                ExpanderSettings.IsExpanded = false;
                ExpanderSettings.IsEnabled = false;
                isInitalized = true;
                await InitalizeService();
            }
        }

        public static string GetComputerName()
        {
            try
            {
                // https://stackoverflow.com/questions/1233217/difference-between-systeminformation-computername-environment-machinename-and
                // Hostname with umlauts won't work with Envoirnment.MachineName (e.g. BšRO-RECHNER)
                // Fallback with localhost/Hostname/then MachineName

                string name = System.Net.Dns.GetHostEntry("localhost").HostName;
                if (string.IsNullOrEmpty(name))
                    name = System.Net.Dns.GetHostName();

                if (!string.IsNullOrEmpty(name))
                    return name.ToUpper();
            }
            catch
            {
                
            }

            return Environment.MachineName;
        }

        private async Task InitalizeService()
        {
            api = new Communication.API(ServiceData.Instance.APIUrl);
            legacyAPI = new LegacyAPI(ServiceData.Instance.APIUrl);

            var now = DateTime.Now;
            currentDevice = new Device()
            {
                ID = ServiceData.Instance.ID,
                IP = NET.DetermineIPAddress(),
                DeviceGroup = ServiceData.Instance.DeviceGroup,
                Location = ServiceData.Instance.Location,
                Name = GetComputerName(),
                OS = ServiceData.Instance.SystemType,
                Type = ServiceData.Instance.Type,
                DiskDrives = JsonConvert.DeserializeObject<List<DiskDrive>>(WMI.DetermineDiskDrives()),
                Environment = new DeviceEnvironment()
                {
                    CPUCount = Environment.ProcessorCount,
                    CPUName = WMI.DetermineCPUName(),
                    TotalRAM = Native.DetermineTotalRAM(),
                    OSName = ServiceData.Instance.OSName,
                    Motherboard = WMI.DetermineMotherboard(),
                    OSVersion = Environment.OSVersion.ToString(),
                    RunningTime = now.Subtract(startTimestamp),
                    StartTimestamp = startTimestamp
                },
                Screens = GetScreenInformation(),
            };

            // Run tick manually on first_start
            if (ServiceData.Instance.HasLoggedInOnce)
                isInitalized = true;

#if LEGACY
            if (!isInitalized)
                isInitalized = legacyAPI.RegisterDeviceAsync(currentDevice);
#else
            if (!isInitalized)
                isInitalized = await api.RegisterDeviceAsync(currentDevice);
#endif

            if (isInitalized)
                await SendAck();

            if (!ServiceData.Instance.HasLoggedInOnce)
                ServiceData.Instance.HasLoggedInOnce = true;

            ackTimer.Tick += AckTimer_Tick;
            ackTimer.Interval = TimeSpan.FromMinutes(1);
            ackTimer.Start();
        }

        private List<Screen> GetScreenInformation()
        {
            List<Screen> screens = new List<Screen>();

            foreach (var screen in WMI.GetScreenInformation())
            {
                screens.Add(new Screen()
                {
                    ID = screen["id"].Value<string>(),
                    Manufacturer = screen["manufacturer"].Value<string>(),
                    Serial = screen["serial"].Value<string>(),
                    BuiltDate  = screen["built_date"].Value<string>(),
                    Index = screen["index"].Value<int>(),
                    IsPrimary = screen["is_primary"].Value<bool>(),
                    DeviceName = screen["device_name"].Value<string>(),
                    Resolution = screen["resolution"].Value<string>(),
                });
            }

            return screens;
        }

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
                // Initalize
#if LEGACY
                isInitalized = legacyAPI.RegisterDeviceAsync(currentDevice);
#else
                isInitalized = await api.RegisterDeviceAsync(currentDevice);
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

 
        private async Task SendAck()
        {
            var now = DateTime.Now;

            currentDevice.IP = NET.DetermineIPAddress();
            currentDevice.DiskDrives = JsonConvert.DeserializeObject<List<DiskDrive>>(WMI.DetermineDiskDrives());
            currentDevice.Environment.RunningTime = now.Subtract(startTimestamp); // Environment.TickCount?
            currentDevice.Environment.OSVersion = Environment.OSVersion.ToString();
            currentDevice.Environment.CPUCount = Environment.ProcessorCount;
            currentDevice.Environment.TotalRAM = Native.DetermineTotalRAM();
            currentDevice.Environment.FreeRAM = Performance.DetermineFreeRAM();
            currentDevice.Environment.CPUUsage = Performance.GetCPUUsage();
            currentDevice.Environment.DiskUsage = Performance.GetDiskUsage();
            currentDevice.Environment.Is64BitOS = Environment.Is64BitOperatingSystem;
            currentDevice.Environment.MachineName = GetComputerName();
            currentDevice.Environment.UserName = Environment.UserName;
            currentDevice.Environment.DomainName = Environment.UserDomainName;
            currentDevice.Environment.GraphicCards = WMI.DetermineGraphicsCardNames();
            currentDevice.ServiceClientVersion = $"vWindows{typeof(MainWindow).Assembly.GetName().Version.ToString(3)}";
            WMI.GetVendorInfo(out string product, out string description, out string vendor);
            currentDevice.Environment.Product = product;
            currentDevice.Environment.Description = description;
            currentDevice.Environment.Vendor = vendor;
            currentDevice.Screens = GetScreenInformation();

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
                    // Show message
                    try
                    {
                        // Notification is in a different folder, otherwise there are problems with different versions of Newtonsoft.JSOn
                        System.Diagnostics.Process.Start(@"Notification\Notification.exe", Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(ackResult.Result.JsonData)));
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
            string fileName = System.IO.Path.Combine(ServiceData.SCREENSHOT_PATH, $"capture{DateTime.Now.ToString(Consts.SCREENSHOT_DATE_FILE_FORMAT)}.png");
            var result = NET.CreateScreenshot(fileName);

#if LEGACY
            var apiResult = legacyAPI.SendScreenshotAsync(new Screenshot() { ClientID = ServiceData.Instance.ID, Data = Convert.ToBase64String(result) });
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

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = ServiceData.Instance.HasLoggedInOnce;
            if (ServiceData.Instance.HasLoggedInOnce)
                WindowState = WindowState.Minimized;
        }   
   
        private async void ButtonInitalize_Click(object sender, RoutedEventArgs e)
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

                await InitalizeService();
                // WindowState = WindowState.Minimized;
                Visibility = Visibility.Hidden;
            }
            else
                MessageBox.Show("Invalid data set", "Invalid data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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