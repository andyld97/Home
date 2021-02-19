using System;

namespace Home.Model
{
    public class Device
    {
        public string Name { get; set; }

        public string IP { get; set; }

        public DateTime LastSeen { get; set; }

        public Status DeviceStatus { get; set; }

        public enum Status
        {
            Active,
            Idle,
            Offline
        }
    }
}
