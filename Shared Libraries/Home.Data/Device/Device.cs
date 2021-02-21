using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Home.Model
{
    public class Device : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string name;
        private string ip;
        private string guid = Guid.NewGuid().ToString();
        private DateTime lastSeen;
        private DeviceType type;
        private DeviceStatus status;
        private OSType os;
        private string deviceGroup;
        private string location;

        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name
        {
            get => name;
            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string ID
        {
            get => guid.ToString();
            set
            {
                if (guid != null)
                {
                    guid = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("ip")]
        [System.Text.Json.Serialization.JsonPropertyName("ip")]
        public string IP
        {
            get => ip;
            set
            {
                if (value != ip)
                {
                    ip = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("last_seen")]
        [System.Text.Json.Serialization.JsonPropertyName("last_seen")]
        public DateTime LastSeen
        {
            get => lastSeen;
            set
            {
                if (value != lastSeen)
                {
                    lastSeen = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("state")]
        [System.Text.Json.Serialization.JsonPropertyName("state")]
        public DeviceStatus Status
        {
            get => status;
            set
            {
                if (value != status)
                {
                    status = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public DeviceType Type
        {
            get => type;
            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("os")]
        [System.Text.Json.Serialization.JsonPropertyName("os")]
        public OSType OS
        {
            get => os;
            set
            {
                if (value != os)
                {
                    os = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("group")]
        [System.Text.Json.Serialization.JsonPropertyName("group")]
        public string DeviceGroup
        {
            get => deviceGroup;
            set
            {
                if (value != deviceGroup)
                {
                    deviceGroup = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("location")]
        [System.Text.Json.Serialization.JsonPropertyName("location")]
        public string Location
        {
            get => location;
            set
            {
                if (value != location)
                {
                    location = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonProperty("log_entries")]
        [System.Text.Json.Serialization.JsonPropertyName("log_entries")]
        public ObservableCollection<string> LogEntries { get; set; } = new ObservableCollection<string>();

        [JsonProperty("environment")]
        [System.Text.Json.Serialization.JsonPropertyName("environment")]
        public DeviceEnvironment Envoirnment { get; set; } = new DeviceEnvironment();

        [JsonProperty("disk_drives")]
        [System.Text.Json.Serialization.JsonPropertyName("disk_drives")]
        public List<DiskDrive> DiskDrives { get; set; } = new List<DiskDrive>();

        public enum DeviceStatus
        {
            Active,
            Idle,
            Offline
        }

        public enum DeviceType
        {
            SingleBoardDevice,
            MiniPC,
            Notebook,
            Desktop,
            Server
        }

        public enum OSType
        {
            Linux,
            LinuxMint,
            LinuxUbuntu,
            WindowsXP,
            WindowsaVista,
            Windows7,
            Windows8,
            Windows10,
            Unix,
            Other
        }

        public string DetermineDeviceImage()
        {
            string image = string.Empty;
            switch (Type)
            {
                case DeviceType.MiniPC:
                case DeviceType.Server:
                case DeviceType.Desktop:
                case DeviceType.Notebook:
                    {
                        string prequel = $"{(Type == DeviceType.Notebook ? "notebook" : "desktop")}_";

                        switch (OS)
                        {
                            case OSType.Linux: prequel += "linux"; break;
                            case OSType.LinuxMint: prequel += "mint"; break;
                            case OSType.LinuxUbuntu: prequel += "ubuntu"; break;
                            case OSType.WindowsXP: prequel += "windows_xp"; break;
                            case OSType.WindowsaVista: prequel += "windows_vista"; break;
                            case OSType.Windows7: prequel += "windows_7"; break;
                            case OSType.Windows8: prequel += "windows_8"; break;
                            case OSType.Windows10: prequel += "windows_10"; break;
                        }

                        image = prequel;
                    }
                    break;
                case DeviceType.SingleBoardDevice: { image = "pi"; } break;
            }

            return $"{image}.png";
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
            IP = other.IP;
            LastSeen = lastSeen;
            Type = other.Type;
            Status = state;
            OS = other.OS;
            DeviceGroup = other.DeviceGroup;
            Location = other.location;
            Envoirnment = other.Envoirnment;
            DiskDrives = other.DiskDrives;

            if (isLocal)
                LogEntries.Clear();

            foreach (var entry in other.LogEntries)
                LogEntries.Add(entry);
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            string rn = System.Environment.NewLine;
            string env = Envoirnment.ToString();

            return $"ID: {guid}{rn}Name: {name}{rn}IP: {ip}{rn}Type: {Type}{rn}Status: {Status}{rn}OS: {OS}{rn}Group: {DeviceGroup}{rn}Location: {location}{rn}{rn}{env}";
        }
    }

    public class DeviceEnvironment
    {
        [JsonProperty("os_name")]
        [System.Text.Json.Serialization.JsonPropertyName("os_name")]
        public string OSName { get; set; }

        [JsonProperty("os_version")]
        [System.Text.Json.Serialization.JsonPropertyName("os_version")]
        public string OSVersion { get; set; }

        [JsonProperty("cpu_name")]
        [System.Text.Json.Serialization.JsonPropertyName("cpu_name")]
        public string CPUName { get; set; }

        [JsonProperty("cpu_count")]
        [System.Text.Json.Serialization.JsonPropertyName("cpu_count")]
        public int CPUCount { get; set; }

        [JsonProperty("cpu_usage")]
        [System.Text.Json.Serialization.JsonPropertyName("cpu_usage")]
        public double CPUUsage { get; set; }

        [JsonProperty("total_ram")]
        [System.Text.Json.Serialization.JsonPropertyName("total_ram")]
        public long TotalRAM { get; set; }

        [JsonProperty("free_ram")]
        [System.Text.Json.Serialization.JsonPropertyName("free_ram")]
        public string FreeRAM { get; set; }

        [JsonProperty("disk_usage")]
        [System.Text.Json.Serialization.JsonPropertyName("disk_usage")]
        public double DiskUsage { get; set; }

        [JsonProperty("is_64bit_os")]
        [System.Text.Json.Serialization.JsonPropertyName("is_64bit_os")]
        public bool Is64BitOS { get; set; }

        [JsonProperty("machine_name")]
        [System.Text.Json.Serialization.JsonPropertyName("machine_name")]
        public string MachineName { get; set; }

        [JsonProperty("user_name")]
        [System.Text.Json.Serialization.JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonProperty("domain_name")]
        [System.Text.Json.Serialization.JsonPropertyName("domain_name")]
        public string DomainName { get; set; }

        [XmlIgnore]
        [JsonIgnore()]
        [System.Text.Json.Serialization.JsonIgnore]
        public TimeSpan RunningTime { get; set; }

        [JsonProperty("running_time")]
        [JsonPropertyName("running_time")]
        public string XmlRunningTime
        {
            get => RunningTime.ToString();
            set => RunningTime = TimeSpan.Parse(value);
        }

        public override string ToString()
        {
            string rn = Environment.NewLine;
            return $"OS: {OSName}{rn}OS-VER: {OSVersion}{rn}CPU: {CPUName}{rn}CPU-COUNT: {CPUCount}{rn}RAM: {TotalRAM}{rn}FREE: {FreeRAM}{rn}Running-Time: {XmlRunningTime}";
        }
    }

    public class DiskDrive
    {
        [JsonProperty("physical_name")]
        [JsonPropertyName("physical_name")]
        public string PhysicalName { get; set; }

        [JsonProperty("disk_name")]
        [JsonPropertyName("disk_name")]
        public string DiskName { get; set; }

        [JsonProperty("disk_model")]
        [JsonPropertyName("disk_model")]
        public string DiskModel { get; set; }

        [JsonProperty("disk_interface")]
        [JsonPropertyName("disk_interface")]
        public string DiskInterface { get; set; }

        [JsonProperty("media_loaded")]
        [JsonPropertyName("media_loaded")]
        public bool MediaLoaded { get; set; }

        [JsonProperty("media_type")]
        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }

        [JsonProperty("media_signature")]
        [JsonPropertyName("media_signature")]
        public uint MediaSignature { get; set; }

        [JsonProperty("media_status")]
        [JsonPropertyName("media_status")]
        public string MediaStatus { get; set; }

        [JsonProperty("drive_name")]
        [JsonPropertyName("drive_name")]
        public string DriveName { get; set; }

        [JsonProperty("drive_id")]
        [JsonPropertyName("drive_id")]
        public string DriveID { get; set; }

        [JsonProperty("drive_compressed")]
        [JsonPropertyName("drive_compressed")]
        public bool DriveCompressed { get; set; }

        [JsonProperty("drive_type")]
        [JsonPropertyName("drive_type")]
        public uint DriveType { get; set; }

        [JsonProperty("file_system")]
        [JsonPropertyName("file_system")]
        public string FileSystem { get; set; }

        [JsonProperty("free_space")]
        [JsonPropertyName("free_space")]
        public ulong FreeSpace { get; set; } // bytes

        [JsonProperty("total_space")]
        [JsonPropertyName("total_space")]
        public ulong TotalSpace { get; set; } // bytes

        [JsonProperty("drive_media_type")]
        [JsonPropertyName("drive_media_type")]
        public uint DriveMediaType { get; set; }

        [JsonProperty("volume_name")]
        [JsonPropertyName("volume_name")]
        public string VolumeName { get; set; }

        [JsonProperty("volume_serial")]
        [JsonPropertyName("volume_serial")]
        public string VolumeSerial { get; set; }

        public override string ToString()
        {
            return $"{PhysicalName}: {VolumeName}";
        }
    }
}
