using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Home.Model.Device;

namespace Home.Service.Windows.Model
{
    public class ServiceData : INotifyPropertyChanged
    {
        public static readonly string DATA_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Home", "Client", "data.xml");
        public static readonly string SCREENSHOT_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Home", "Client", "Screenshots");

        public static readonly ServiceData Instance = ServiceData.Load();

        static ServiceData()
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DATA_PATH));
            }
            catch
            { }

            try
            {
                System.IO.Directory.CreateDirectory(SCREENSHOT_PATH);
            }
            catch
            { }
        }

        private string id = Guid.NewGuid().ToString();
        private string deviceGroup;
        private string location;
        private string osName;
        private bool hasLoggedIn = false;
        private bool postScreenshots = true;
        private OSType osType;
        private DeviceType deviceType;
        private string apiURL = "http://192.168.178.38:83";
        private DateTime lastUpdateCheck = DateTime.MinValue;
        public event PropertyChangedEventHandler PropertyChanged;

        public string ID 
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    NotifyPropertyChanged(nameof(ID));
                }
            }
        }

        public string OSName
        {
            get => osName;
            set
            {
                if (osName != value)
                {
                    osName = value;
                    NotifyPropertyChanged(nameof(OSName));
                }
            }
        }

        public string APIUrl
        {
            get => apiURL;
            set
            {
                if (value != apiURL)
                {
                    apiURL = value;
                    NotifyPropertyChanged(nameof(APIUrl));
                }
            }
        }

        public OSType SystemType
        {
            get => osType;
            set
            {
                if (osType != value)
                {
                    osType = value;
                    NotifyPropertyChanged(nameof(SystemType));
                }
            }
        }

        public DeviceType Type
        {
            get => deviceType;
            set
            {
                if (deviceType != value)
                {
                    deviceType = value;
                    NotifyPropertyChanged(nameof(Type));
                }
            }
        }

        public bool HasLoggedInOnce
        {
            get => hasLoggedIn;
            set
            {
                if (hasLoggedIn != value)
                {
                    hasLoggedIn = value;
                    NotifyPropertyChanged(nameof(HasLoggedInOnce));
                }
            }
        }

        public bool PostScreenshots
        {
            get => postScreenshots;
            set
            {
                if (postScreenshots != value)
                {
                    postScreenshots = value;
                    NotifyPropertyChanged(nameof(PostScreenshots)); 
                }
            }
        }

        public string DeviceGroup
        {
            get => deviceGroup;
            set
            {
                if (deviceGroup != value)
                {
                    deviceGroup = value;
                    NotifyPropertyChanged(nameof(DeviceGroup));
                }
            }
        }

        public string Location
        {
            get => location;
            set
            {
                if (location != value)
                {
                    location = value;
                    NotifyPropertyChanged(nameof(Location));
                }
            }
        }

        public DateTime LastUpdateCheck
        {
            get => lastUpdateCheck;
            set
            {
                if (lastUpdateCheck != value)
                {
                    lastUpdateCheck = value;
                    NotifyPropertyChanged(nameof(LastUpdateCheck));
                }
            }
        }

        public static ServiceData Load()
        {
            try
            {
                if (System.IO.File.Exists(DATA_PATH))
                {
                    var instance = Serialization.Serialization.Read<ServiceData>(DATA_PATH, Serialization.Serialization.Mode.Normal);
                    if (instance != null)
                        return instance;
                }
            }
            catch
            {

            }

            return new ServiceData();
        }

        public void NotifyPropertyChanged(string propertyName) // cannot use [CallerMemberName] due to compatibility issues
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (Instance != null)
                Save();
        }

        public void Save()
        {
            try
            {
                Serialization.Serialization.Save(DATA_PATH, this, Serialization.Serialization.Mode.Normal);
            }
            catch
            {

            }
        }
    }
}