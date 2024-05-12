using Home.Data;

#if !LEGACY
using Home.Data.Com;
#endif
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
#if !LEGACY
using System.Text.Json.Serialization;
using System.Text.Json;
using Home.Data.Helper;
#endif
using System.Xml.Serialization;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using System.Text;
using System.Linq;
using System.Diagnostics.SymbolStore;
using System.Reflection;
#if !LEGACY
using Units;
#endif

namespace Home.Model
{
    public class Device : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string name;
        private string ip;
        private string macAddress;
        private string guid = Guid.NewGuid().ToString();
        private DateTime lastSeen;
        private DeviceType type;
        private DeviceStatus status;
        private OSType os;
        private string deviceGroup;
        private string location;
        private bool? isLive;
        private string serviceClientVersion;
        private DeviceEnvironment environment = new DeviceEnvironment();

        [JsonProperty("name")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("name")]
#endif
        public string Name
        {
            get => name;
            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [JsonProperty("id")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("id")]
#endif
        public string ID
        {
            get => guid?.ToString();
            set
            {
                if (guid != value)
                {
                    guid = value;
                    OnPropertyChanged(nameof(ID));
                }
            }
        }

        [JsonProperty("ip")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("ip")]
#endif
        public string IP
        {
            get => ip;
            set
            {
                if (value != ip)
                {
                    ip = value;
                    OnPropertyChanged(nameof(IP));
                }
            }
        }

        [JsonProperty("mac_address")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("mac_address")]
#endif
        public string MacAddress
        {
            get => macAddress;
            set
            {
                if (value != macAddress)
                {
                    macAddress = value;
                    OnPropertyChanged(nameof(MacAddress));
                }
            }
        }

        [JsonProperty("last_seen")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("last_seen")]
#endif
        public DateTime LastSeen
        {
            get => lastSeen;
            set
            {
                if (value != lastSeen)
                {
                    lastSeen = value;
                    OnPropertyChanged(nameof(LastSeen));
                }
            }
        }

        [JsonProperty("state")]
         #if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("state")]
        #endif
        public DeviceStatus Status
        {
            get => status;
            set
            {
                if (value != status)
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        [JsonProperty("type")]
         #if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        #endif
        public DeviceType Type
        {
            get => type;
            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        [JsonProperty("os")]
         #if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("os")]
        #endif
        public OSType OS
        {
            get => os;
            set
            {
                if (value != os)
                {
                    os = value;
                    OnPropertyChanged(nameof(OS));
                }
            }
        }

        [JsonProperty("group")]
         #if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("group")]
        #endif
        public string DeviceGroup
        {
            get => deviceGroup;
            set
            {
                if (value != deviceGroup)
                {
                    deviceGroup = value;
                    OnPropertyChanged(nameof(DeviceGroup));
                }
            }
        }

        [JsonProperty("location")]
         #if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("location")]
        #endif
        public string Location
        {
            get => location;
            set
            {
                if (value != location)
                {
                    location = value;
                    OnPropertyChanged(nameof(Location));
                }
            }
        }

        /// <summary>
        /// Determines if this device is being watched.
        /// It's not really live, but if this property is true, the device will be forced to send a screenshot in every ack!
        /// </summary>
        [JsonProperty("is_live")]
        #if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("is_live")]
        #endif
        [XmlIgnore] 
        // Ignore (won't save) because on api start we do not know any clients which may be still using this
        // to prevent that if there are no clients that we generate unnecessary data
        public bool? IsLive
        {
            get => isLive;
            set
            {
                if (value != isLive)
                {
                    isLive = value;
                    OnPropertyChanged(nameof(IsLive));
                }
            }
        }

        [JsonProperty("bios")]
#if !LEGACY
        [JsonPropertyName("bios")]
#endif
        public BIOS BIOS { get; set; }

        [JsonProperty("log_entries")]
         #if !LEGACY
        [JsonPropertyName("log_entries")]
        #endif
        public ObservableCollection<LogEntry> LogEntries { get; set; } = new ObservableCollection<LogEntry>();

        [JsonProperty("screenshots")]
         #if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("screenshots")]
        #endif
        public ObservableCollection<Screenshot> Screenshots { get; set; } = new ObservableCollection<Screenshot>();

        [JsonProperty("environment")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("environment")]
#endif
        public DeviceEnvironment Environment
        { 
            get => environment; 
            set
            {
                if (value != environment)
                {
                    environment = value;
                    OnPropertyChanged(nameof(Environment));
                }
            }
        }

        [JsonProperty("disk_drives")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("disk_drives")]
#endif
        public ObservableCollection<DiskDrive> DiskDrives { get; set; } = new ObservableCollection<DiskDrive>();

        [JsonProperty("service_client_version")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("service_client_version")]
#endif
        public string ServiceClientVersion
        {
            get => serviceClientVersion;
            set
            {
                if (value != serviceClientVersion)
                {
                    serviceClientVersion = value;
                    OnPropertyChanged(nameof(ServiceClientVersion));
                }
            }
        }

        [JsonProperty("usage")]
#if !LEGACY
        [JsonPropertyName("usage")]
#endif
        public DeviceUsage Usage { get; set; } = new DeviceUsage();

        [JsonProperty("screens")]
#if !LEGACY
        [JsonPropertyName("screens")]
#endif
        public ObservableCollection<Screen> Screens { get; set; } = new ObservableCollection<Screen>();

#if !LEGACY
        #region Properties for Internal API Usage
        /// <summary>
        /// Only for internal api usage
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsScreenshotRequired { get; set; }

        /// <summary>
        /// Only for internal api usage
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [XmlIgnore]
        public List<Message> Messages { get; set; } = new List<Message>();

        /// <summary>
        /// Only for internal api usage
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [XmlIgnore]
        public List<Command> Commands { get; set; } = new List<Command>();

        #endregion

        [JsonPropertyName("storage_warnings")]
        public ObservableCollection<StorageWarning> StorageWarnings { get; set; } = new ObservableCollection<StorageWarning>();

        [JsonPropertyName("device_changes")]
        public ObservableCollection<DeviceChangeEntry> DevicesChanges { get; set; } = new ObservableCollection<DeviceChangeEntry>();

        /// <summary>
        /// The battery warning for this device (if null there is no warning)
        /// </summary>
        [JsonProperty("battery_warning")]
#if !LEGACY
        [JsonPropertyName("battery_warning")]
#endif
        public BatteryWarning BatteryWarning { get; set; } = null;

#endif

        /// <summary>
        /// The battery info for this device (if null there is no battery)
        /// </summary>
        [JsonProperty("battery_info")]
#if !LEGACY
        [JsonPropertyName("battery_info")]
#endif
        public Battery BatteryInfo { get; set; }

        public enum DeviceStatus
        {
            Active,
            Idle,
            Offline
        }

        public enum DeviceType
        {
            [Description("Single-Board-Device")]
            SingleBoardDevice,

            [Description("Mini PC")]
            MiniPC,

            [Description("Notebook")]
            Notebook,

            [Description("Desktop")]
            Desktop,

            [Description("Server")]
            Server,

            [Description("Smartphone")]
            Smartphone,

            [Description("Smart TV")]
            SmartTV,

            [Description("Set Top Box")]
            SetTopBox,

            [Description("Tablet")]
            Tablet,

            [Description("Virtual Machine")]
            VirtualMachine,

            [Description("Android TV Stick")]
            AndroidTVStick
        }

        public enum OSType
        {
            [Description("Android")]
            Android,

            [Description("Linux")]
            Linux,

            [Description("Mint (Linux)")]
            LinuxMint,

            [Description("Ubuntu (Linux)")]
            LinuxUbuntu,

            [Description("Windows XP")]
            WindowsXP,

            [Description("Windows Vista")]
            WindowsVista,

            [Description("Windows 7")]
            Windows7,

            [Description("Windows 8")]
            Windows8,

            [Description("Windows 10")]
            Windows10,

            [Description("Windows 11")]
            Windows11,

            [Description("Unix")]
            Unix,

            [Description("Other")]
            Other
        }

        public string DetermineDeviceImage()
        {
            string image = string.Empty;
            switch (Type)
            {
                case DeviceType.Tablet: image = "tablet"; break;
                case DeviceType.Smartphone: image = "smartphone"; break;
                case DeviceType.SmartTV: image = "smarttv"; break;
                case DeviceType.SetTopBox: image = "settopbox"; break;
                case DeviceType.AndroidTVStick: image = "androidtvstick"; break;
                case DeviceType.MiniPC:
                case DeviceType.Server:
                case DeviceType.Desktop:
                case DeviceType.Notebook:
                    {
                        string prequel;
                        if (Type == DeviceType.Notebook)
                            prequel = "notebook";
                        else if (Type == DeviceType.Server)
                            prequel = "server";
                        else
                            prequel = "desktop";

                        prequel += "_";

                        switch (OS)
                        {
                            case OSType.Linux: prequel += "linux"; break;
                            case OSType.LinuxMint: prequel += "mint"; break;
                            case OSType.LinuxUbuntu: prequel += "ubuntu"; break;
                            case OSType.WindowsXP: prequel += "windows_xp"; break;
                            case OSType.WindowsVista: prequel += "windows_vista"; break;
                            case OSType.Windows7: prequel += "windows_7"; break;
                            case OSType.Windows8: prequel += "windows_8"; break;
                            case OSType.Windows10: prequel += "windows_10"; break;
                            case OSType.Windows11: prequel += "windows_11"; break;
                        }

                        image = prequel;
                    }
                    break;
                case DeviceType.SingleBoardDevice: { image = "pi"; } break;
            }

            if ((Status == DeviceStatus.Offline) && (Type == DeviceType.Notebook || Type == DeviceType.Desktop || Type == DeviceType.Smartphone || Type == DeviceType.Tablet || Type == DeviceType.MiniPC))
                image = $"offline/{image}";

            return $"{image}.png";
        }

        public System.IO.Stream GetImage(string image = "")
        {
            if (string.IsNullOrEmpty(image))
                image = DetermineDeviceImage();

            image = image.Replace("/", ".");
            return typeof(Device).Assembly.GetManifestResourceStream($"Home.Data.resources.icons.devices.{image}");
        }

        public static List<Device> GenerateSampleDevices()
        {
            var now = DateTime.Now;

            List<Device> result = new List<Device>
            {
                new Device()
                {
                    Status = Device.DeviceStatus.Active,
                    IP = "192.168.178.2",
                    LastSeen = now.AddHours(-5),
                    Name = "Andy-PC",
                    Location = "Andys-Zimmer",
                    Type = DeviceType.Desktop,
                    OS = OSType.Windows10,
                },
                new Device()
                {
                    Status = Device.DeviceStatus.Active,
                    IP = "192.168.178.39",
                    LastSeen = now,
                    Name = "srv01",
                    Location = "Andys-Zimmer",
                    DeviceGroup = "Server",
                    Type = DeviceType.Server,
                    OS = OSType.LinuxUbuntu
                },
                new Device()
                {
                    Status = Device.DeviceStatus.Offline,
                    IP = "192.168.178.40",
                    LastSeen = now.AddDays(-5),
                    Name = "ArbeitskellerR",
                    Location = "Arbeitskeller",
                    Type = DeviceType.Desktop,
                    OS = OSType.Windows8
                },
                new Device()
                {
                    Status = DeviceStatus.Idle,
                    IP = "192.168.178.47",
                    LastSeen = now,
                    Location = "Keller",
                    Name = "DSSK",
                    OS = OSType.Linux,
                    Type = DeviceType.SingleBoardDevice,
                },
                new Device()
                {
                    Status = DeviceStatus.Offline,
                    IP = "Unknown",
                    LastSeen = now,
                    OS = OSType.WindowsXP,
                    Name = "MT7",
                    Type = DeviceType.Desktop,
                    Location = "Andys-Zimmer"
                },
                new Device()
                {
                    Status = DeviceStatus.Offline,
                    IP = "Unknown",
                    LastSeen = now,
                    OS = OSType.Windows7,
                    Name = "MT14 (weiß)",
                    Type = DeviceType.Desktop,
                    Location = "Andys-Zimmer"
                },
                new Device()
                {
                    Status = DeviceStatus.Offline,
                    IP = "Unknown",
                    LastSeen = now,
                    OS = OSType.Windows8,
                    Name = "MT14 (schwarz)",
                    Type = DeviceType.Desktop,
                    Location = "Andys-Zimmer"
                },
            };

            return result;
        }

        public void Update(Device other, DateTime lastSeen, DeviceStatus state, bool isLocal = false)
        {
            Name = other.Name;
            
            // Only update if IP isn't empty
            if (!string.IsNullOrEmpty(other.IP))
                IP = other.IP;

            // Only update if MacAddress isn't empty
            if (!string.IsNullOrEmpty(other.MacAddress))
                MacAddress = other.MacAddress;

            LastSeen = lastSeen;
            Type = other.Type;
            Status = state;
            OS = other.OS;
            DeviceGroup = other.DeviceGroup;
            Location = other.location;

            if (other.BIOS != null)
            {
                if (BIOS == null)
                    BIOS = new BIOS();

                BIOS.Vendor = other.BIOS.Vendor;
                BIOS.Description = other.BIOS.Description;
                BIOS.Version = other.BIOS.Version;
                BIOS.ReleaseDate = other.BIOS.ReleaseDate;
            }

            // Update environment
            Environment.CPUCount = other.Environment.CPUCount;
            Environment.CPUName = other.Environment.CPUName;
            Environment.CPUUsage = other.Environment.CPUUsage;
            Environment.Description = other.Environment.Description;
            Environment.DiskUsage = other.Environment.DiskUsage;
            Environment.DomainName = other.Environment.DomainName;
            Environment.AvailableRAM = other.Environment.AvailableRAM;
#pragma warning disable CS0612 // Typ oder Element ist veraltet
            Environment.Graphics = other.Environment.Graphics;
#pragma warning restore CS0612 // Typ oder Element ist veraltet
            Environment.Is64BitOS = other.Environment.Is64BitOS;
            Environment.MachineName = other.Environment.MachineName;
            Environment.Motherboard = other.Environment.Motherboard;
            Environment.OSName = other.Environment.OSName;
            Environment.OSVersion = other.Environment.OSVersion;
            Environment.Product = other.Environment.Product;
            Environment.RunningTime = other.Environment.RunningTime;
            Environment.StartTimestamp = other.Environment.StartTimestamp;
            Environment.TotalRAM = other.Environment.TotalRAM;
            Environment.UserName = other.Environment.UserName;
            Environment.Vendor = other.Environment.Vendor;             

            Environment.GraphicCards.Clear();
            foreach (var card in other.Environment.GraphicCards)
                Environment.GraphicCards.Add(card);

            DiskDrives.Clear();
            foreach (var disk in other.DiskDrives)
                DiskDrives.Add(disk);
            ServiceClientVersion = other.ServiceClientVersion;
            BatteryInfo = other.BatteryInfo;

#if !LEGACY
            // Don't update StorageWarnings because it will only be set via API and not via clients - except it is used locally in Home GUI App
            if (isLocal)
            {
                StorageWarnings = other.StorageWarnings;
                BatteryWarning = other.BatteryWarning;
            }
#endif

            if (IP.EndsWith("/24"))
                IP = IP.Replace("/24", string.Empty);

#pragma warning disable CS0612 // Typ oder Element ist veraltet
            if (Environment.GraphicCards.Count == 0 && !string.IsNullOrEmpty(other.Environment.Graphics))
                Environment.GraphicCards.Add(other.Environment.Graphics);
#pragma warning restore CS0612 // Typ oder Element ist veraltet

#if !LEGACY
            DevicesChanges.Clear();
            foreach (var change in other.DevicesChanges.OrderByDescending(c => c.Timestamp))
                DevicesChanges.Add(change);
#endif

            // In an API Context we should not update the usage here
            if (isLocal)
                Usage = other.Usage;

            // Only update if value != null to keep old versions compatible (they may don't have this property yet)
            if (other.IsLive != null)
                IsLive = other.IsLive;

            if (other.Screenshots.Count > 0)
            {
                Screenshots.Clear();
                foreach (var shot in other.Screenshots)
                    Screenshots.Add(shot);
            }

            if (other.Screens.Count > 0)
            {
                Screens.Clear();
                foreach (var screen in other.Screens)
                    Screens.Add(screen);
            }

#if !LEGACY
            IsScreenshotRequired = other.IsScreenshotRequired;
#endif

            if (isLocal)
                LogEntries.Clear();

            foreach (var entry in other.LogEntries)
                LogEntries.Add(entry);
        }

        public void OnPropertyChanged(string propertyName) // Cannot use [CallerMemberName] due to compatibility issues
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            string rn = System.Environment.NewLine;
            string env = Environment.ToString();

            return $"ID: {guid}{rn}Name: {name}{rn}IP: {ip}{rn}Type: {Type}{rn}Status: {Status}{rn}OS: {OS}{rn}Group: {DeviceGroup}{rn}Location: {location}{rn}{rn}{env}";
        }
    }

    public class DeviceEnvironment : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string product;
        private string description;
        private string vendor;
        private string osName;
        private string osVersion;
        private string cpuName;
        private int cpuCount;
        
        private double cpuUsage;
        private double diskUsage;

        private string motherboard;
        private string graphics;
        private double totalRAM;
        private double availableRAM = 0;
        
        private string machineName;
        private string userName;
        private string domainName;

        private bool is64BitOS;
        private TimeSpan runningTime;
        private DateTime startTimestamp;

        [JsonProperty("product")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("product")]
#endif
        public string Product
        {
            get => product;
            set
            {
                if (value != product)
                {
                    product = value;
                    OnPropertyChanged(nameof(Product));
                }
            }
        }

        [JsonProperty("description")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("description")]
#endif
        public string Description
        {
            get => description;
            set
            {
                if (value != description)
                {
                    description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        [JsonProperty("vendor")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("vendor")]
#endif
        public string Vendor
        {
            get => vendor;
            set
            {
                if (value != vendor)
                {
                    vendor = value;
                    OnPropertyChanged(nameof(Vendor));
                }
            }
        }

        [JsonProperty("os_name")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("os_name")]
#endif
        public string OSName
        {
            get => osName;
            set 
            {
                if (value != osName)
                {
                    osName = value;
                    OnPropertyChanged(nameof(OSName));
                }
            }
        }

        [JsonProperty("os_version")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("os_version")]
#endif
        public string OSVersion
        {
            get => osVersion;
            set
            {
                if (value != osVersion)
                {
                    osVersion = value;
                    OnPropertyChanged(nameof(OSVersion));
                }
            }
        }

        [JsonProperty("cpu_name")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("cpu_name")]
#endif
        public string CPUName
        {
            get => cpuName;
            set
            {
                if (value != cpuName)
                {
                    cpuName = value;
                    OnPropertyChanged(nameof(CPUName));
                }
            }
        }

        [JsonProperty("cpu_count")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("cpu_count")]
#endif
        public int CPUCount
        {
            get => cpuCount;
            set
            {
                if (value != cpuCount)
                {
                    cpuCount = value;
                    OnPropertyChanged(nameof(CPUCount));
                }
            }
        }

        [JsonProperty("cpu_usage")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("cpu_usage")]
#endif
        public double CPUUsage
        {
            get => cpuUsage;
            set
            {
                if (value != cpuUsage)
                {
                    cpuUsage= value;
                    OnPropertyChanged(nameof(CPUUsage));
                }
            }
        }

        [JsonProperty("motherboard")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("motherboard")]
#endif
        public string Motherboard
        {
            get => motherboard;
            set
            {
                if (value != motherboard)
                {
                    motherboard = value;
                    OnPropertyChanged(nameof(Motherboard));
                }
            }
        }

        [Obsolete()]
        [JsonProperty("graphics")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("graphics")]
#endif
        public string Graphics
        {
            get => graphics;
            set
            {
                if (value != graphics)
                {
                    graphics = value;
                    OnPropertyChanged(nameof(Graphics));
                }
            }
        }

        [JsonProperty("graphic_cards")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("graphic_cards")]
#endif
        public ObservableCollection<string> GraphicCards { get; set; } = new ObservableCollection<string>();

        [JsonProperty("total_ram")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("total_ram")]
#endif
        public double TotalRAM
        {
            get => totalRAM;
            set
            {
                if (value != totalRAM)
                {
                    totalRAM = value;
                    OnPropertyChanged(nameof(TotalRAM));
                }
            }
        }

        [JsonProperty("available_ram")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("available_ram")]
#endif
        public double AvailableRAM
        {
            get => availableRAM;
            set
            {
                if (value != availableRAM)
                {
                    availableRAM = value;
                    OnPropertyChanged(nameof(availableRAM));
                }
            }
        }

        [JsonProperty("free_ram")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("free_ram")]
#endif
        [Obsolete("Use AvailableRAM instead!")]
        public string FreeRAM
        {
            get => AvailableRAM.ToString();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Example: 4.21 GB used (53 %)
                    var index = value.IndexOf("GB");
                    try
                    {
                        if (index != -1 && double.TryParse(value.Substring(0, index), out double parsed))
                            AvailableRAM = Math.Max(TotalRAM - parsed, 0);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        [JsonProperty("disk_usage")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("disk_usage")]
#endif
        public double DiskUsage
        {
            get => diskUsage;
            set
            {
                if (value != diskUsage)
                {
                    diskUsage = value;
                    OnPropertyChanged(nameof(DiskUsage));
                }
            }
        }

        [JsonProperty("is_64bit_os")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("is_64bit_os")]
#endif
        public bool Is64BitOS
        {
            get => is64BitOS;
            set
            {
                if (value != is64BitOS)
                {
                    is64BitOS = true;
                    OnPropertyChanged(nameof(Is64BitOS));
                }
            }
        }

        [JsonProperty("machine_name")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("machine_name")]
#endif
        public string MachineName
        {
            get => machineName;
            set
            {
                if (value != machineName)
                {
                    machineName = value;
                    OnPropertyChanged(nameof(MachineName));
                }
            }
        }

        [JsonProperty("user_name")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("user_name")]
#endif
        public string UserName
        {
            get => userName;
            set
            {
                if (value != userName)
                {
                    userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }

        [JsonProperty("domain_name")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("domain_name")]
#endif
        public string DomainName
        {
            get => domainName;
            set
            {
                if (value != domainName)
                {
                    domainName = value;
                    OnPropertyChanged(nameof(DomainName));
                }
            }
        }

        [XmlIgnore]
        [JsonIgnore()]
#if !LEGACY
        [System.Text.Json.Serialization.JsonIgnore]
#endif
        public TimeSpan RunningTime
        {
            get => runningTime;
            set
            {
                if (value != runningTime)
                {
                    runningTime = value;
                    OnPropertyChanged(nameof(RunningTime));
                }
            }
        }

        [JsonProperty("running_time")]
#if !LEGACY
        [JsonPropertyName("running_time")]
#endif
        public string XmlRunningTime
        {
            get => RunningTime.ToString();
            set => RunningTime = TimeSpan.Parse(value);
        }

        [JsonProperty("start_time")]
#if !LEGACY
        [JsonPropertyName("start_time")]
#endif
        public DateTime StartTimestamp
        {
            get => startTimestamp;
            set
            {
                if (value != startTimestamp)
                {
                    startTimestamp = value;
                    OnPropertyChanged(nameof(StartTimestamp));
                }
            }
        }

        public override string ToString()
        {
            string rn = Environment.NewLine;
#pragma warning disable CS0612 // Typ oder Element ist veraltet
            string graphics = GraphicCards.Count == 0 ? Graphics : string.Join(Environment.NewLine, GraphicCards);
#pragma warning restore CS0612 // Typ oder Element ist veraltet
            string cpuName = CPUName;

            if (cpuName.Contains(Environment.NewLine))
                cpuName = cpuName.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            return $"OS: {OSName}{rn}OS-VER: {OSVersion}{rn}CPU: {cpuName}{rn}CPU-COUNT: {CPUCount}{rn}Motherboard: {Motherboard}{rn}Graphics: {graphics}{rn}RAM: {TotalRAM} GB{rn}AVAIL: {AvailableRAM}GB{rn}Running-Time: {XmlRunningTime}";
        }

        public void OnPropertyChanged(string propertyName) // Cannot use [CallerMemberName] due to compatibility issues
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DiskDrive
    {
        private string cachedId = string.Empty;

        [JsonProperty("physical_name")]
#if !LEGACY
        [JsonPropertyName("physical_name")]
#endif
        public string PhysicalName { get; set; }

        [JsonProperty("disk_name")]
#if !LEGACY
        [JsonPropertyName("disk_name")]
#endif
        public string DiskName { get; set; }

        [JsonProperty("disk_model")]
#if !LEGACY
        [JsonPropertyName("disk_model")]
#endif
        public string DiskModel { get; set; }

        [JsonProperty("disk_interface")]
#if !LEGACY
        [JsonPropertyName("disk_interface")]
#endif
        public string DiskInterface { get; set; }

        [JsonProperty("media_loaded")]
#if !LEGACY
        [JsonPropertyName("media_loaded")]
#endif
        public bool MediaLoaded { get; set; }

        [JsonProperty("media_type")]
#if !LEGACY
        [JsonPropertyName("media_type")]
#endif
        public string MediaType { get; set; }

        [JsonProperty("media_signature")]
#if !LEGACY
        [JsonPropertyName("media_signature")]
#endif
        public ulong MediaSignature { get; set; }

        [JsonProperty("media_status")]
#if !LEGACY
        [JsonPropertyName("media_status")]
#endif
        public string MediaStatus { get; set; }

        [JsonProperty("drive_name")]
#if !LEGACY
        [JsonPropertyName("drive_name")]
#endif
        public string DriveName { get; set; }

        [JsonProperty("drive_id")]
#if !LEGACY
        [JsonPropertyName("drive_id")]
#endif
        public string DriveID { get; set; }

        [JsonProperty("drive_compressed")]
#if !LEGACY
        [JsonPropertyName("drive_compressed")]
#endif
        public bool DriveCompressed { get; set; }

        [JsonProperty("drive_type")]
#if !LEGACY
        [JsonPropertyName("drive_type")]
#endif
        public uint DriveType { get; set; }

        [JsonProperty("file_system")]
#if !LEGACY
        [JsonPropertyName("file_system")]
#endif
        public string FileSystem { get; set; }

        [JsonProperty("free_space")]
#if !LEGACY
        [JsonPropertyName("free_space")]
#endif
        public ulong FreeSpace { get; set; } // bytes

        [JsonProperty("total_space")]
#if !LEGACY
        [JsonPropertyName("total_space")]
#endif
        public ulong TotalSpace { get; set; } // bytes

        [JsonProperty("drive_media_type")]
#if !LEGACY
        [JsonPropertyName("drive_media_type")]
#endif
        public uint DriveMediaType { get; set; }

        [JsonProperty("volume_name")]
#if !LEGACY
        [JsonPropertyName("volume_name")]
#endif
        public string VolumeName { get; set; }

        [JsonProperty("volume_serial")]
#if !LEGACY
        [JsonPropertyName("volume_serial")]
#endif
        public string VolumeSerial { get; set; }

#if !LEGACY
        /// <summary>
        /// A unique id for this disk (hash) calculated by all "non-changing" properties
        /// </summary>
        [JsonIgnore()]
        public string UniqueID
        {
            get
            {
                if (!string.IsNullOrEmpty(cachedId))
                    return cachedId;

                // Useful properties for a unique id:
                /*  PhysicalName,
                    DiskName,
                    DiskModel,
                    DiskInterface,
                    MediaType,
                    MediaSignature,
                    DriveName,
                    DriveID,
                    DriveType,
                    FileSystem,
                    TotalSpace,
                    DriveMediaType,
                    VolumeName,
                    VolumeSerial
                */
                StringBuilder builder = new StringBuilder();
                builder.Append(PhysicalName);
                builder.Append(DiskName);
                builder.Append(DiskModel);
                builder.Append(DiskInterface);
                builder.Append(MediaType);
                builder.Append(MediaSignature);
                builder.Append(DriveName);
                builder.Append(DriveID);
                builder.Append(DriveType);
                builder.Append(FileSystem);
                builder.Append(TotalSpace);
                builder.Append(DriveMediaType);
                builder.Append(VolumeName);
                builder.Append(VolumeSerial);

                cachedId = builder.ToString().BuildSHA1Hash();
                return cachedId;
            }
        }
#endif


#if !LEGACY
        /// <summary>
        /// Determines if the disk is full
        /// </summary>
        /// <param name="percentageFree"></param>
        /// <returns>true if the disk is full according to percentageFree</returns>
        public bool? IsFull(double percentageFree = 10)
        {
            if (TotalSpace == 0)
                return null;

            if (FreeSpace == 0)
                return false;

            if (percentageFree == 0.0)
                return FreeSpace == 0.0;

            var total = ByteUnit.FromB((long)TotalSpace);
            var free = ByteUnit.FromB((long)FreeSpace);

            return free.Length <= (total.Length * (percentageFree / 100.0));
        }
#endif

        public override string ToString()
        {
            return $"{PhysicalName}: {VolumeName}";
        }
    }

    public class Screenshot
    {
        [JsonProperty("data")]
#if !LEGACY
        [JsonPropertyName("data")]
#endif
        public string Data { get; set; }

        [JsonProperty("client_id")]
#if !LEGACY
        [JsonPropertyName("client_id")]
#endif
        public string DeviceID { get; set; }

        [JsonProperty("filename")]
#if !LEGACY
        [JsonPropertyName("filename")]
#endif
        public string Filename { get; set; }

        [JsonProperty("timestamp")]
#if !LEGACY
        [JsonPropertyName("timestamp")]
#endif
        public DateTime? Timestamp { get; set; }

        [JsonProperty("screen_index")]
#if !LEGACY
        [JsonPropertyName("screen_index")]
#endif
        public int? ScreenIndex { get; set; }
    }

    public class DeviceUsage
    {
        [JsonProperty("cpu")]
#if !LEGACY
        [JsonPropertyName("cpu")]
#endif
        public List<double> CPU { get; set; } = new List<double>();

        [JsonProperty("ram")]
#if !LEGACY
        [JsonPropertyName("ram")]
#endif
        public List<double> RAM { get; set; } = new List<double>();

        [JsonProperty("disk")]
#if !LEGACY
        [JsonPropertyName("disk")]
#endif
        public List<double> DISK { get; set; } = new List<double>();

        [JsonProperty("battery")]
#if !LEGACY
        [JsonPropertyName("battery")]
#endif
        public List<int> Battery { get; set; } = new List<int>(); // percentage

        public void Clear()
        {
            CPU.Clear();
            RAM.Clear();
            DISK.Clear();
            Battery.Clear();
        }

        public void AddCPUEntry(double value)
        {
            EnsureListHasEnoughSpace(CPU);
            CPU.Add(value);
        }

        public void AddRAMEntry(double value)
        {
            EnsureListHasEnoughSpace(RAM);
            RAM.Add(value);
        }

        public void AddDISKEntry(double value)
        {
            EnsureListHasEnoughSpace(DISK);
            DISK.Add(value);
        }

        public void AddBatteryEntry(int value)
        {
            EnsureListHasEnoughSpace(Battery);
            Battery.Add(value);

            // Notice: for example a smartphone sometimes doesn't sends the ack, due to energy savings.
            // So, the values might not be fully accurate on android (to be so, an ack timestamp is required, but it is currently not supported)
        }

        private void EnsureListHasEnoughSpace<T>(List<T> list)
        {
            if (list.Count >= 60)
            {
                while (list.Count != 59)
                    list.RemoveAt(0);
            }
        }
    }

    public class Battery
    {
        /// <summary>
        /// Determines if the device is currently charging
        /// </summary>
        [JsonProperty("is_charging")]
#if !LEGACY
        [JsonPropertyName("is_charging")]
#endif
        public bool IsCharging { get; set; }

        /// <summary>
        /// Determines the remaining battery percentage 
        /// </summary>
        [JsonProperty("battery_level")]
#if !LEGACY
        [JsonPropertyName("battery_level")]
#endif
        public int BatteryLevelInPercent { get; set; }
    }

    public class Screen
    {
        [JsonProperty("id")]
#if !LEGACY
        [JsonPropertyName("id")]
#endif
        public string ID { get; set; }

        [JsonProperty("serial")]
#if !LEGACY
        [JsonPropertyName("serial")]
#endif
        public string Serial { get; set; }

        [JsonProperty("built_date")]
#if !LEGACY
        [JsonPropertyName("built_date")]
#endif
        public string BuiltDate { get; set; }

        [JsonProperty("manufacturer")]
#if !LEGACY
        [JsonPropertyName("manufacturer")]
#endif
        public string Manufacturer { get; set; }

        [JsonProperty("index")]
#if !LEGACY
        [JsonPropertyName("index")]
#endif
        public int Index { get; set; }

        [JsonProperty("is_primary")]
#if !LEGACY
        [JsonPropertyName("is_primary")]
#endif
        public bool IsPrimary { get; set; }

        [JsonProperty("device_name")]
#if !LEGACY
        [JsonPropertyName("device_name")]
#endif
        public string DeviceName { get; set; }

        [JsonProperty("resolution")]
#if !LEGACY
        [JsonPropertyName("resolution")]
#endif
        public string Resolution { get; set; }
    }

    public class BIOS
    {
        [JsonProperty("vendor")]
#if !LEGACY
        [JsonPropertyName("vendor")]
#endif
        public string Vendor { get; set; }

        [JsonProperty("version")]
#if !LEGACY
        [JsonPropertyName("version")]
#endif
        public string Version { get; set; }

        [JsonProperty("description")]
#if !LEGACY
        [JsonPropertyName("description")]
#endif
        public string Description { get; set; }

        [JsonProperty("release_date")]
#if !LEGACY
        [JsonPropertyName("release_date")]
#endif
        public DateTime ReleaseDate { get; set; }

#if !LEGACY
        public string BuildHash()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Vendor);
            sb.Append(Version);
            sb.Append(Description);
            if (ReleaseDate > DateTime.MinValue)    
                sb.Append(ReleaseDate.ToString("s"));

            return sb.ToString().BuildSHA1Hash();
        }

        public static bool operator ==(BIOS b1, BIOS b2)
        {
            return b1?.BuildHash() == b2?.BuildHash();
        }

        public static bool operator !=(BIOS b1, BIOS b2)
        {
            return (b1?.BuildHash() != b2?.BuildHash()); 
        }

        public override int GetHashCode()
        {
            return BuildHash().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BIOS b2)
                return this == b2;

            return false;
        }

#endif

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Vendor);
            sb.Append($" ({Version})");
            if (ReleaseDate > DateTime.MinValue)
            {
                sb.Append(Environment.NewLine);
                sb.Append($"Release-Date: {ReleaseDate.ToShortDateString()}");
            }

            return sb.ToString();
        }
    }


#if !LEGACY

    public class DeviceChangeEntry
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("type")]
        public DeviceChangeType Type { get; set; } 

        public enum DeviceChangeType
        {
            CPU = 0,
            RAM = 1,
            Motherboard = 2,
            Graphics = 3,
            OS = 4,
            IP = 5,
            DiskDrive = 6,
            BIOS = 7,

            None = 10000
        }
    }

    public abstract class Warning<T>
    {
        /// <summary>
        /// The timestamp when this warning firstly occurred
        /// </summary>
        [JsonPropertyName("warning_occoured")]
        public DateTime WarningOccurred { get; set; }

        [XmlIgnore]
        [JsonPropertyName("text")]
        public abstract string Text { get; }

        [JsonIgnore()]
        public abstract string Name { get; }

        /// <summary>
        /// Checks if the storage warning is obsolete
        /// </summary>
        /// <param name="dd"></param>
        /// <param name="percentage">Percentage</param>
        /// <param name="offsetPercentage">A value that is added to percentage to prevent continuously adding/removing the warning if the value is on the boundary</param>
        /// <returns>true if the warning is obsolete</returns>
        public abstract bool CanBeRemoved(T param, int percentage, int offsetPercentage = 10);

        /// <summary>
        /// Create a log entry of this warning
        /// </summary>
        /// <returns></returns>
        public LogEntry ConvertToLogEntry(string deviceName)
        {
            string message = $"[{Name} Warning]: Created for {deviceName} - \"{Text}\"";
            return new LogEntry(WarningOccurred, message, LogEntry.LogLevel.Warning, true);
        }
    }

    public enum WarningType
    {
        StorageWarning = 0,
        BatteryWarning = 1,
    }

    public class StorageWarning : Warning<DiskDrive>
    {
        /// <summary>
        /// The id of the related DiskDrive
        /// </summary>
        [JsonPropertyName("storage_id")]
        public string StorageID { get; set; }  

        [JsonPropertyName("value")]
        public ulong Value { get; set; }

        [JsonPropertyName("disk_name")]
        public string DiskName { get; set; }

        [JsonIgnore()]
        public override string Name => "Storage";

        [XmlIgnore]
        public override string Text => $"DISK: \"{DiskName}\" is low on storage. Free space left: {ByteUnit.FindUnit(Value)}";

        public override bool CanBeRemoved(DiskDrive dd, int percentage, int offsetPercentage = 10)
        {
            if (dd == null)
                throw new ArgumentNullException("dd");

            if (dd.UniqueID != StorageID)
                throw new ArgumentException("DiskDrive with the wrong id specified!");

            var result = dd.IsFull(percentage + offsetPercentage);
            return (result == null || result.HasValue && !result.Value);
        }

        /// <summary>
        /// Creates a storage warning with the given text at exactly THIS moment (timestamp)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static StorageWarning Create(string diskName, string storageID, ulong value)
        {
            StorageWarning storageWarning = new StorageWarning();
            storageWarning.StorageID = storageID;
            storageWarning.WarningOccurred = DateTime.Now;
            storageWarning.Value = value;
            storageWarning.DiskName = diskName;

            return storageWarning;
        }
    }

    public class BatteryWarning : Warning<Device>
    {
        [JsonIgnore()]
        public override string Name => "Battery";

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [XmlIgnore]
        public override string Text => $"Battery is low: {Value}% left!";

        public override bool CanBeRemoved(Device param, int percentage, int offsetPercentage)
        {
            if (param.BatteryInfo == null)
                return true;

            if (param.BatteryInfo.IsCharging)
                return true;

            return CanBeRemoved(param.BatteryInfo.BatteryLevelInPercent, percentage + offsetPercentage);
        }

        public bool CanBeRemoved(int devicePercentage, int percentage)
        {
            return devicePercentage > percentage;
        }

        /// <summary>
        /// Creates a battery warning with the given text at exactly THIS moment (timestamp)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static BatteryWarning Create(int value)
        {
            BatteryWarning batteryWarning = new BatteryWarning();
            batteryWarning.WarningOccurred = DateTime.Now;
            batteryWarning.Value = value;

            return batteryWarning;
        }
    }

#endif
}