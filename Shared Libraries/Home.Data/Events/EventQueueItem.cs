﻿using Home.Model;
using Newtonsoft.Json;
using System;

namespace Home.Data.Events
{
    public class EventQueueItem
    {
        [JsonProperty("related_device_id")]
        [System.Text.Json.Serialization.JsonPropertyName("related_device_id")]
        public string DeviceID { get; set; }

        [JsonProperty("event_occured")]
        [System.Text.Json.Serialization.JsonPropertyName("event_occured")]
        public DateTime EventOccured { get; set; }

        [JsonProperty("event_description")]
        [System.Text.Json.Serialization.JsonPropertyName("event_description")]
        public EventKind EventDescription { get; set; }

        [JsonProperty("event_data")]
        [System.Text.Json.Serialization.JsonPropertyName("event_data")]
        public EventData EventData { get; set; }

        public enum EventKind
        {
            /// <summary>
            /// Denotes that a new device is connected/registered in the system
            /// </summary>
            NewDeviceConnected,

            /// <summary>
            /// Denotes that the event device just started
            /// </summary>
            DeviceLoggedIn,

            /// <summary>
            /// Denotes that the event device changed it's state to offline/online
            /// </summary>
            DeviceChangedState,

            /// <summary>
            /// Device ACK will be received every minute with full information of the device
            /// </summary>
            ACK,

            /// <summary>
            /// Denotes that the API just received a screenshot of this device
            /// </summary>
            DeviceScreenshotRecieved,

            /// <summary>
            /// Denotes that the API received log entries related to the event device
            /// </summary>
            LogEntriesRecieved,

            /// <summary>
            /// Denotes that the event device reported a critical error
            /// </summary>
            ErrorReported,

            /// <summary>
            /// Denotes that the log of the event device got cleared
            /// </summary>
            LogCleared,

            /// <summary>
            /// Denotes that the hardware change log of the event device got cleared
            /// </summary>
            HardwareChangesCleared,

            /// <summary>
            /// Denotes that the "live mode" of the event device got changed
            /// </summary>
            LiveModeChanged,

            /// <summary>
            /// Denotes that a device was deleted
            /// </summary>
            DeviceDeleted,
        }
    }

    public class EventData
    {
        [JsonProperty("event_device")]
        [System.Text.Json.Serialization.JsonPropertyName("event_device")]
        public Device EventDevice { get; set; } // maybe null

        public EventData()
        { }

        public EventData(Device eventDevice)
        {
            EventDevice = eventDevice;
        }
    }
}
