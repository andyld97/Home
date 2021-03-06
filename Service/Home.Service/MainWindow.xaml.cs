using Home.Data.Com;
using Home.Model;
using Home.Service.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static Home.Model.Device;

namespace Home.Service
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Home.Communication.API api = null;
        private readonly DateTime startTimestamp = DateTime.Now;

        private readonly PerformanceCounter cpuCounter;
        private readonly PerformanceCounter ramCounter;
        private readonly PerformanceCounter diskCounter;

        private Device currentDevice = null;
        private readonly DispatcherTimer ackTimer = new DispatcherTimer();
        private bool isInitalized = false;
        private bool isSendingAck = false;
        private readonly object _lock = new object();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

        public MainWindow()
        {
            InitializeComponent();
            api = new Communication.API("http://localhost:5000");

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

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

        private async Task InitalizeService()
        {
            api = new Communication.API(ServiceData.Instance.APIUrl);
            var now = DateTime.Now;
            currentDevice = new Device()
            {
                ID = ServiceData.Instance.ID,
                IP = DisplayIPAddresses(),
                DeviceGroup = ServiceData.Instance.DeviceGroup,
                Location = ServiceData.Instance.Location,
                Name = Environment.MachineName,
                OS = ServiceData.Instance.SystemType,
                Type = ServiceData.Instance.Type,
                DiskDrives = DetermineDiskDrives(),
                Envoirnment = new DeviceEnvironment()
                {
                    CPUCount = Environment.ProcessorCount,
                    CPUName = DetermineCPUName(),
                    TotalRAM = DetermineTotalRAM(),
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
                isInitalized = await api.RegisterDeviceAsync(currentDevice);

            if (isInitalized)
                await SendAck();

            if (!ServiceData.Instance.HasLoggedInOnce)
                ServiceData.Instance.HasLoggedInOnce = true;

            ackTimer.Tick += AckTimer_Tick;
            ackTimer.Interval = TimeSpan.FromMinutes(1);
            ackTimer.Start();
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
                isInitalized = await api.RegisterDeviceAsync(currentDevice);
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


        /*
         * 
         * https://stackoverflow.com/a/41841500/6237448
         * 
         * 
         * 
         *  PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter("Processor", "% Privileged Time", "_Total");
            PerformanceCounter("Processor", "% Interrupt Time", "_Total");
            PerformanceCounter("Processor", "% DPC Time", "_Total");
            PerformanceCounter("Memory", "Available MBytes", null);
            PerformanceCounter("Memory", "Committed Bytes", null);
            PerformanceCounter("Memory", "Commit Limit", null);
            PerformanceCounter("Memory", "% Committed Bytes In Use", null);
            PerformanceCounter("Memory", "Pool Paged Bytes", null);
            PerformanceCounter("Memory", "Pool Nonpaged Bytes", null);
            PerformanceCounter("Memory", "Cache Bytes", null);
            PerformanceCounter("Paging File", "% Usage", "_Total");
            PerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", "_Total");
            PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total");
            PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", "_Total");
            PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            PerformanceCounter("Process", "Handle Count", "_Total");
            PerformanceCounter("Process", "Thread Count", "_Total");
            PerformanceCounter("System", "Context Switches/sec", null);
            PerformanceCounter("System", "System Calls/sec", null);
            PerformanceCounter("System", "Processor Queue Length", null);
        */

        private async Task SendAck()
        {
            var now = DateTime.Now;

            currentDevice.IP = DisplayIPAddresses();
            currentDevice.DiskDrives = DetermineDiskDrives();
            currentDevice.Envoirnment.RunningTime = now.Subtract(startTimestamp); // Environment.TickCount?
            currentDevice.Envoirnment.OSVersion = Environment.OSVersion.ToString();
            currentDevice.Envoirnment.CPUCount = Environment.ProcessorCount;
            currentDevice.Envoirnment.TotalRAM = DetermineTotalRAM();
            currentDevice.Envoirnment.FreeRAM = DetermineFreeRAM();
            currentDevice.Envoirnment.CPUUsage = Math.Round(cpuCounter.NextValue(), 0);
            currentDevice.Envoirnment.DiskUsage = Math.Round(diskCounter.NextValue(), 0);
            currentDevice.Envoirnment.Is64BitOS = Environment.Is64BitOperatingSystem;
            currentDevice.Envoirnment.MachineName = Environment.MachineName;
            currentDevice.Envoirnment.UserName = Environment.UserName;
            currentDevice.Envoirnment.DomainName = Environment.UserDomainName;
            currentDevice.Envoirnment.Graphics = GetGraphics();
            currentDevice.ServiceClientVersion = $"v{typeof(MainWindow).Assembly.GetName().Version.ToString(3)}";
            GetDeviceInfo(currentDevice);

            // Send ack
            var ackResult = await api.SendAckAsync(currentDevice);

            // Process ack answer
            if (ackResult != null && ackResult.Success && ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.OK))
            {
                if (ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.ScreenshotRequired))
                    await CreateScreenshot();

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

                if (ackResult.Result.Result.HasFlag(AckResult.Ack.CommandRecieved))
                {
                    // ToDO: 
                }
            }
        }

        public async Task CreateScreenshot()
        {
            try
            {
                string filename = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"capture{DateTime.Now:ddMMyyyy-hhmmss}.png");

                double screenLeft = SystemParameters.VirtualScreenLeft;
                double screenTop = SystemParameters.VirtualScreenTop;
                double screenWidth = SystemParameters.VirtualScreenWidth;
                double screenHeight = SystemParameters.VirtualScreenHeight;

                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)screenWidth, (int)screenHeight))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {                      
                        g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                        bmp.Save(filename);
                    }
                }

                byte[] result = System.IO.File.ReadAllBytes(filename);
                try
                {
                    System.IO.File.Delete(filename);
                }
                catch
                {

                }

                var apiResult = await api.SendScreenshotAsync(new Screenshot() { ClientID = ServiceData.Instance.ID, Data = Convert.ToBase64String(result) });

                // ToDO: log
            }
            catch (Exception ex)
            {
                // ToDo: Log
            }
        }

        public string GetGraphics()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DisplayConfiguration");

                string graphicsCard = string.Empty;
                foreach (ManagementObject mo in searcher.Get())
                {
                    foreach (PropertyData property in mo.Properties)
                    {
                        if (property.Name == "Description")
                        {
                            return property.Value.ToString();
                        }
                    }
                }
            }
            catch
            {

            }

            return string.Empty;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = ServiceData.Instance.HasLoggedInOnce;
            if (ServiceData.Instance.HasLoggedInOnce)
                WindowState = WindowState.Minimized;
        }

        private double DetermineTotalRAM()
        {
            try
            {
                GetPhysicallyInstalledSystemMemory(out long memKb);
                return Math.Round(memKb / 1024.0 / 1024.0, 2);
            }
            catch
            {
                return 0;
            }
        }

        private string DetermineFreeRAM()
        {
            long freeGB = (long)(ramCounter.NextValue() / 1024);
            double totalGB = DetermineTotalRAM();
            double usedGB = totalGB - freeGB;

            int percentage = (int)Math.Round((usedGB / totalGB) * 100);

            return $"{usedGB} GB used ({percentage} %)";
        }

        private string DetermineCPUName()
        {
            try
            {
                ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
                foreach (ManagementObject mo in mos.Get())
                {
                    string value = mo["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(value))
                        return value.Trim();
                }
            }
            catch
            {
                // Log
            }

            return string.Empty;
        }

        public static void GetDeviceInfo(Device device)
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct");

                foreach (ManagementObject mo in searcher.Get())
                {
                    foreach (PropertyData property in mo.Properties)
                    {
                        if (property.Name == "Name")
                            device.Envoirnment.Product = property.Value?.ToString();

                        else if (property.Name == "Description")
                            device.Envoirnment.Description = property.Value?.ToString();

                        else if (property.Name == "Vendor")
                            device.Envoirnment.Vendor = property.Value?.ToString();
                    }
                }
            }
            catch
            {

            }

        }

        public static string DisplayIPAddresses()
        {
            string returnAddress = string.Empty;

            try
            {
                // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface network in networkInterfaces)
                {
                    // Read the IP configuration for each network
                    IPInterfaceProperties properties = network.GetIPProperties();

                    string description = network.Description.ToLower();

                    if (network.NetworkInterfaceType == NetworkInterfaceType.Ethernet && network.OperationalStatus == OperationalStatus.Up && !description.Contains("virtual") && !description.Contains("pseudo"))
                    {
                        // Each network interface may have multiple IP addresses
                        foreach (IPAddressInformation address in properties.UnicastAddresses)
                        {
                            // We're only interested in IPv4 addresses for now
                            if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                                continue;

                            // Ignore loopback addresses (e.g., 127.0.0.1)
                            if (IPAddress.IsLoopback(address.Address))
                                continue;

                            returnAddress = address.Address.ToString();
                        }
                    }
                }
            }
            catch
            {

            }

            return returnAddress;
        }

        private List<DiskDrive> DetermineDiskDrives()
        {
            List<DiskDrive> result = new List<DiskDrive>();

            try
            {
                var driveQuery = new ManagementObjectSearcher("select * from Win32_DiskDrive");
                foreach (ManagementObject d in driveQuery.Get())
                {
                    var deviceId = d.Properties["DeviceId"].Value;
                    var partitionQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_DiskDriveToDiskPartition", d.Path.RelativePath);
                    var partitionQuery = new ManagementObjectSearcher(partitionQueryText);

                    try
                    {
                        foreach (ManagementObject p in partitionQuery.Get())
                        {
                            var logicalDriveQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_LogicalDiskToPartition", p.Path.RelativePath);
                            var logicalDriveQuery = new ManagementObjectSearcher(logicalDriveQueryText);

                            foreach (ManagementObject ld in logicalDriveQuery.Get())
                            {
                                try
                                {
                                    var physicalName = Convert.ToString(d.Properties["Name"].Value); // \\.\PHYSICALDRIVE2
                                    var diskName = Convert.ToString(d.Properties["Caption"].Value); // WDC WD5001AALS-xxxxxx
                                    var diskModel = Convert.ToString(d.Properties["Model"].Value); // WDC WD5001AALS-xxxxxx
                                    var diskInterface = Convert.ToString(d.Properties["InterfaceType"].Value); // IDE
                                    var capabilities = (ushort[])d.Properties["Capabilities"].Value; // 3,4,7 - random access, supports writing, 7=removable device
                                    var mediaLoaded = Convert.ToBoolean(d.Properties["MediaLoaded"].Value); // bool
                                    var mediaType = Convert.ToString(d.Properties["MediaType"].Value); // Fixed hard disk media
                                    var mediaSignature = Convert.ToUInt32(d.Properties["Signature"].Value); // int32
                                    var mediaStatus = Convert.ToString(d.Properties["Status"].Value); // OK

                                    var driveName = Convert.ToString(ld.Properties["Name"].Value); // C:
                                    var driveId = Convert.ToString(ld.Properties["DeviceId"].Value); // C:
                                    var driveCompressed = Convert.ToBoolean(ld.Properties["Compressed"].Value);
                                    var driveType = Convert.ToUInt32(ld.Properties["DriveType"].Value); // C: - 3
                                    var fileSystem = Convert.ToString(ld.Properties["FileSystem"].Value); // NTFS
                                    var freeSpace = Convert.ToUInt64(ld.Properties["FreeSpace"].Value); // in bytes
                                    var totalSpace = Convert.ToUInt64(ld.Properties["Size"].Value); // in bytes
                                    var driveMediaType = Convert.ToUInt32(ld.Properties["MediaType"].Value); // c: 12
                                    var volumeName = Convert.ToString(ld.Properties["VolumeName"].Value); // System
                                    var volumeSerial = Convert.ToString(ld.Properties["VolumeSerialNumber"].Value); // 12345678

                                    DiskDrive dd = new DiskDrive()
                                    {
                                        PhysicalName = physicalName,
                                        DiskName = diskName,
                                        DiskModel = diskModel,
                                        DiskInterface = diskInterface,
                                        MediaLoaded = mediaLoaded,
                                        MediaType = mediaType,
                                        MediaSignature = mediaSignature,
                                        MediaStatus = mediaStatus,
                                        DriveName = driveName,
                                        DriveID = driveId,
                                        DriveCompressed = driveCompressed,
                                        DriveType = driveType,
                                        FileSystem = fileSystem,
                                        FreeSpace = freeSpace,
                                        TotalSpace = totalSpace,
                                        DriveMediaType = driveMediaType,
                                        VolumeName = volumeName,
                                        VolumeSerial = volumeSerial
                                    };

                                    result.Add(dd);
                                }
                                catch (Exception ex)
                                {
                                    // ToDO: Log
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // ToDO: Log
                    }
                }
            }
            catch (Exception ex)
            {
                // ToDO: Log
            }

            return result.OrderBy(p => p.DriveID).ToList();
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
}
