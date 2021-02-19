using System;
using System.Collections.Generic;

namespace Home.Model
{
    public class Device
    {
        public string Name { get; set; }

        public string IP { get; set; }

        public DateTime LastSeen { get; set; }

        public Status DeviceStatus { get; set; }

        public DeviceType Type { get; set; }

        public OSType OS { get; set; }

        public string LastScreenshotFileName { get; set; }

        public string DeviceGroup { get; set; }

        public string Location { get; set; }

        public enum Status
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
                    DeviceStatus = Device.Status.Active,
                    IP = "192.168.178.2",
                    LastSeen = now.AddHours(-5),
                    Name = "Andy-PC",
                    Location = "Andys-Zimmer",
                    Type = DeviceType.Desktop,
                    OS = OSType.Windows10,
                },
                new Device()
                {
                    DeviceStatus = Device.Status.Active,
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
                    DeviceStatus = Device.Status.Offline,
                    IP = "192.168.178.40",
                    LastSeen = now.AddDays(-5),
                    Name = "ArbeitskellerR",
                    Location = "Arbeitskeller",
                    Type = DeviceType.Desktop,
                    OS = OSType.Windows8
                },
                new Device()
                {
                    DeviceStatus = Status.Idle,
                    IP = "192.168.178.47",
                    LastSeen = now,
                    Location = "Keller",
                    Name = "DSSK",
                    OS = OSType.Linux,
                    Type = DeviceType.SingleBoardDevice,
                },
                new Device()
                {
                    DeviceStatus = Status.Offline,
                    IP = "Unknown",
                    LastSeen = now,
                    OS = OSType.WindowsXP,
                    Name = "MT7",
                    Type = DeviceType.Desktop,
                    Location = "Andys-Zimmer"
                },
                new Device()
                {
                    DeviceStatus = Status.Offline,
                    IP = "Unknown",
                    LastSeen = now,
                    OS = OSType.Windows7,
                    Name = "MT14 (weiß)",
                    Type = DeviceType.Desktop,
                    Location = "Andys-Zimmer"
                },
                new Device()
                {
                    DeviceStatus = Status.Offline,
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
    }

    public class System
    {
        public string OSName { get; set; }

        public string OSVersion { get; set; }

    }

    public class Hardware
    {
        public string CPUName { get; set; }

        public int CPUCount { get; set; }

        public long TotalRAM { get; set; }

        public long FreeRAM { get; set; }

    }
}
