using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace Home.Model
{
#if !LEGACY

    public class DeviceSchedulingRule : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string associatedDeviceId;
        private string name;
        private string description;
        private bool isActive;
        private string customMacAddress;

        [JsonPropertyName("associated_device_id")]
        public string AssociatedDeviceId
        {
            get => associatedDeviceId;
            set
            {
                if (value != associatedDeviceId)
                {
                    associatedDeviceId = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("name")]
        public string Name
        {
            get => name;
            set
            {
                if (value != name)
                {
                    name = value;
                    NotifyPropertyChanged(); 
                }
            }
        }

        [JsonPropertyName("description")]
        public string Description
        {
            get => description; 
            set
            {
                if (value != description) 
                {
                    description = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("is_active")]
        public bool IsActive
        {
            get => isActive;
            set
            {
                if (value != isActive)
                {
                    isActive = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("custom_mac_address")]
        public string CustomMacAddress
        {
            get => customMacAddress;
            set
            {
                if (value != customMacAddress)
                {
                    customMacAddress = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("shutdown_rule")]
        public ShutdownRule ShutdownRule { get; set; } = new ShutdownRule();

        [JsonPropertyName("boot_rule")]
        public BootRule BootRule { get; set; } = new BootRule();
       
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static IEnumerable<DeviceSchedulingRule> Load(string fileName)
        {
            if (!System.IO.File.Exists(fileName))
                return new DeviceSchedulingRule[0];

            string json = System.IO.File.ReadAllText(fileName);
            return System.Text.Json.JsonSerializer.Deserialize<IEnumerable<DeviceSchedulingRule>>(json);
        }

        public static void Save(IEnumerable<DeviceSchedulingRule> rules, string fileName)
        {
            System.IO.File.WriteAllText(fileName, System.Text.Json.JsonSerializer.Serialize(rules));
        }
    }

    public class ShutdownRule : INotifyPropertyChanged
    {
        private string time = "00:00";
        private ShutdownRuleType type = ShutdownRuleType.Shutdown;

        /// <summary>
        /// Should be formatted like 00:00!
        /// </summary>
        [JsonPropertyName("time")]
        public string Time
        {
            get => time;
            set
            {
                if (value != time)
                {
                    time = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("type")]
        public ShutdownRuleType Type
        {
            get => type;
            set
            {
                if (value != type)
                {
                    type = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("rule_api_call_info")]
        public RuleAPICallInfo RuleAPICallInfo { get; set; } = new RuleAPICallInfo();

        [JsonPropertyName("rule_command_info")]
        public RuleCommandInfo RuleCommandInfo { get; set; } = new RuleCommandInfo();

        public enum ShutdownRuleType
        {
            None,
            Shutdown,
            Reboot,
            ExecuteCommand,
            ExternalAPICall
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BootRule : INotifyPropertyChanged
    {
        private string time = "00:00";
        private BootRuleType type = BootRuleType.WakeOnLan;

        /// <summary>
        /// Should be formatted like 00:00!
        /// </summary>
        [JsonPropertyName("time")]
        public string Time
        {
            get => time;
            set
            {
                if (value != time)
                {
                    time = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("type")]
        public BootRuleType Type
        {
            get => type;
            set
            {
                if (value != type)
                {
                    type = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("rule_api_call_info")]
        public RuleAPICallInfo RuleAPICallInfo { get; set; } = new RuleAPICallInfo();

        public enum BootRuleType
        {
            None,
            WakeOnLan,
            ExternalAPICall
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RuleAPICallInfo : INotifyPropertyChanged
    {
        private string url;
        private Method httpMethod = Method.GET;

        [JsonPropertyName("url")]
        public string Url
        {
            get => url;
            set
            {
                if (value != url)
                {
                    url = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("http_method")]
        public Method HttpMethod
        {
            get => httpMethod;
            set
            {
                if (value != httpMethod) 
                {
                    httpMethod = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public enum Method
        {
            GET,
            POST,
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RuleCommandInfo : INotifyPropertyChanged
    {
        private string executable, parameter;

        [JsonPropertyName("executable")]
        public string Executable
        {
            get => executable; 
            set
            {
                if (value != executable)
                {
                    executable = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [JsonPropertyName("parameter")]
        public string Parameter
        {
            get => parameter; 
            set
            {
                if (value != parameter)
                {
                    parameter = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
#endif
}