using Home.Data;
using Home.Data.Com;
using Home.Measure.Windows;
using Home.Model;
using Home.Service.Model;
using Newtonsoft.Json;
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

        private void InitalizeService()
        {
            api = new API(ServiceData.Instance.APIUrl);

            var now = DateTime.Now;
            currentDevice = new Device()
            {
                ID = ServiceData.Instance.ID,
                IP = NET.DetermineIPAddress(),
                DeviceGroup = ServiceData.Instance.DeviceGroup,
                Location = ServiceData.Instance.Location,
                Name = Environment.MachineName,
                OS = ServiceData.Instance.SystemType,
                Type = ServiceData.Instance.Type,
                DiskDrives = JsonConvert.DeserializeObject<List<DiskDrive>>(WMI.DetermineDiskDrives()),
                Envoirnment = new DeviceEnvironment()
                {
                    CPUCount = Environment.ProcessorCount,
                    CPUName = WMI.DetermineCPUName(),
                    Motherboard = WMI.DetermineMotherboard(),
                    TotalRAM = Native.DetermineTotalRAM(),
                    OSName = ServiceData.Instance.OSName,
                    OSVersion = Environment.OSVersion.ToString(),
                    RunningTime = now.Subtract(startTimestamp),
                    StartTimestamp = startTimestamp
                }
            };

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

            currentDevice.IP = NET.DetermineIPAddress();
            currentDevice.DiskDrives = JsonConvert.DeserializeObject<List<DiskDrive>>(WMI.DetermineDiskDrives());
            currentDevice.Envoirnment.RunningTime = now.Subtract(startTimestamp); // Environment.TickCount?
            currentDevice.Envoirnment.OSVersion = Environment.OSVersion.ToString();
            currentDevice.Envoirnment.CPUCount = Environment.ProcessorCount;
            currentDevice.Envoirnment.TotalRAM = Native.DetermineTotalRAM();
            currentDevice.Envoirnment.FreeRAM = Performance.DetermineFreeRAM();
            currentDevice.Envoirnment.CPUUsage = Performance.GetCPUUsage();
            currentDevice.Envoirnment.DiskUsage = Performance.GetDiskUsage();
            currentDevice.Envoirnment.Is64BitOS = Environment.Is64BitOperatingSystem;
            currentDevice.Envoirnment.MachineName = Environment.MachineName;
            currentDevice.Envoirnment.UserName = Environment.UserName;
            currentDevice.Envoirnment.DomainName = Environment.UserDomainName;
            currentDevice.Envoirnment.Graphics = WMI.GetGraphics();
            currentDevice.ServiceClientVersion = $"vLegacy{typeof(MainWindow).Assembly.GetName().Version.ToString(3)}";
            WMI.GetVendorInfo(out string product, out string description, out string vendor);
            currentDevice.Envoirnment.Product = product;
            currentDevice.Envoirnment.Description = description;
            currentDevice.Envoirnment.Vendor = vendor;

            // Send ack
            var ackResult = api.SendAckAsync(currentDevice);

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

        public void PostScreenshot()
        {
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"capture{DateTime.Now.ToString(Consts.SCREENSHOT_DATE_FILE_FORMAT)}.png");
            var result = NET.CreateScreenshot(fileName);
            var apiResult = api.SendScreenshotAsync(new Screenshot() { ClientID = ServiceData.Instance.ID, Data = Convert.ToBase64String(result) });
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
            OSType os = (CmbOS.SelectedIndex == 0 ? OSType.WindowsXP : OSType.WindowsaVista);
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
                MessageBox.Show("Invalid data set", "Invalid data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
