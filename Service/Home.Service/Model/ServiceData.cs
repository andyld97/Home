using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Home.Model.Device;

namespace Home.Service.Model
{
    public class ServiceData : INotifyPropertyChanged
    {
        public static readonly string DATA_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.xml");
        public static readonly ServiceData Instance = ServiceData.Load();

        private string id = Guid.NewGuid().ToString();
        private string deviceGroup;
        private string location;
        private string osName;
        private bool hasLoggedIn = false;
        private OSType osType;
        private DeviceType deviceType;
        private string apiURL = "http://192.168.178.38:83";
        public event PropertyChangedEventHandler PropertyChanged;

        public string ID 
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
