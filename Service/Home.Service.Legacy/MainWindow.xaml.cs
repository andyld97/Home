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
using System.Windows;
using System.Windows.Threading;
using static Home.Model.Device;

namespace Home.Service.Legacy
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private Home.Communication.API api = null;
        private readonly DateTime startTimestamp = DateTime.Now;

        private Device currentDevice = null;
        private readonly DispatcherTimer ackTimer = new DispatcherTimer();
        private bool isInitalized = false;
        private bool isSendingAck = false;
        private API api = new API(ServiceData.Instance.APIUrl);
        private readonly object _lock = new object();

        public MainWindow()
        {
            InitializeComponent();

            CmbDeviceType.Items.Clear();
            CmbDeviceType.ItemsSource = Enum.GetValues(typeof(Device.DeviceType));
            CmbDeviceType.SelectedIndex = 0;
           // CmbOS.Items.Clear();
           // CmbOS.ItemsSource = Enum.GetValues(typeof(Device.OSType));
            CmbOS.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (ServiceData.Instance.HasLoggedInOnce)
            {
                // WindowState = WindowState.Minimized;
                Visibility = Visibility.Hidden;
                ExpanderSettings.IsExpanded = false;
                ExpanderSettings.IsEnabled = false;
                isInitalized = true;
                InitalizeService();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            if (e.ExceptionObject != null)
                MessageBox.Show((e.ExceptionObject as Exception).ToString());
#endif

            // Debug:
            // MessageBox.Show("Debug");
        }

        private void InitalizeService()
        {            
            api = new API(ServiceData.Instance.APIUrl);
            var address = NET.DetermineIPAddress();

            var now = DateTime.Now;
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
                    Motherboard = WMI.DetermineMotherboard(),
                    TotalRAM = Native.DetermineTotalRAM(),
                    OSName = NET.GetOsFriendlyName(ServiceData.Instance.OSName),
                    OSVersion = Environment.OSVersion.ToString(),
                    RunningTime = now.Subtract(startTimestamp),
                    StartTimestamp = startTimestamp
                }
            };

            // currentDevice.Screens = GetScreenInformation();

            // Run tick manually on first_start
            if (ServiceData.Instance.HasLoggedInOnce)
                isInitalized = true;

            if (!isInitalized)
                isInitalized = api.RegisterDeviceAsync(currentDevice);

            if (isInitalized)
                SendAck();

            if (!ServiceData.Instance.HasLoggedInOnce)
                ServiceData.Instance.HasLoggedInOnce = true;

            ackTimer.Tick += AckTimer_Tick;
            ackTimer.Interval = TimeSpan.FromMinutes(1);
            ackTimer.Start();
        }

        private void AckTimer_Tick(object sender, EventArgs e)
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
                isInitalized = api.RegisterDeviceAsync(currentDevice);
            }
            else
            {
                // Send ack
                SendAck();
            }


            lock (_lock)
            {
                isSendingAck = false;
            }
        }

        private void SendAck()
        {
            var now = DateTime.Now;
            var address = NET.DetermineIPAddress();
            currentDevice.IP = address.IpAddress;
            currentDevice.MacAddress = address.MacAddress;
            currentDevice.DiskDrives = new System.Collections.ObjectModel.ObservableCollection<DiskDrive>(JsonConvert.DeserializeObject<List<DiskDrive>>(WMI.DetermineDiskDrives()));
            currentDevice.Environment.RunningTime = now.Subtract(startTimestamp); // Environment.TickCount?
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
            currentDevice.ServiceClientVersion = $"vLegacy{typeof(MainWindow).Assembly.GetName().Version.ToString(3)}";
            WMI.GetVendorInfo(out string product, out string description, out string vendor);
            currentDevice.Environment.Product = product;
            currentDevice.Environment.Description = description;
            currentDevice.Environment.Vendor = vendor;
            //currentDevice.Screens = GetScreenInformation();

            bool batteryResult = Home.Measure.Windows.NET.DetermineBatteryInfo(out int batteryPercentage, out bool isCharging);
            if (batteryResult)
                currentDevice.BatteryInfo = new Battery() { BatteryLevelInPercent = batteryPercentage, IsCharging = isCharging };

            // Send ack
            var ackResult = api.SendAckAsync(currentDevice);
            if (ackResult != null && !ackResult.Success)
                System.Diagnostics.Trace.TraceError("Failed to send ack: " + ackResult.ErrorMessage);

            // Process ack answer
            if (ackResult != null && ackResult.Success && ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.OK))
            {
                if (ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.ScreenshotRequired))
                    PostScreenshot();

                if (ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.MessageRecieved))
                {
                    // Show message
                    try
                    {
                        System.Diagnostics.Process.Start("Notification.exe", Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(ackResult.Result.JsonData)));
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
                    BuiltDate = screen["built_date"].Value<string>(),
                    Index = screen["index"].Value<int>(),
                    IsPrimary = screen["is_primary"].Value<bool>(),
                    DeviceName = screen["device_name"].Value<string>(),
                    Resolution = screen["resolution"].Value<string>(),
                });
            }

            return screens;
        }

        public void PostScreenshot()
        {
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"capture{DateTime.Now.ToString(Consts.SCREENSHOT_DATE_FILE_FORMAT)}.png");
            var result = NET.CreateScreenshot(fileName);
            var apiResult = api.SendScreenshotAsync(new Screenshot() { DeviceID = ServiceData.Instance.ID, Data = Convert.ToBase64String(result) });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = ServiceData.Instance.HasLoggedInOnce;
            if (ServiceData.Instance.HasLoggedInOnce)
                WindowState = WindowState.Minimized;
        }

        private void ButtonInitalize_Click(object sender, RoutedEventArgs e)
        {
            // Apply settings
            string host = TextAPIUrl.Text;
            OSType os = (CmbOS.SelectedIndex == 0 ? OSType.WindowsXP : OSType.WindowsVista);
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

                InitalizeService();
                // WindowState = WindowState.Minimized;
                Visibility = Visibility.Hidden;
            }
            else
                MessageBox.Show(Properties.Resources.strInvalidDataSet, Home.Service.Legacy.Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
