using Home.Model;
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
            NewDeviceConnected,
            DeviceLoggedIn,
            DeviceChangedState,
            ACK,
            DeviceScreenshotRecieved,
            LogEntriesRecieved,
            ErrorReported,
            LogCleared,
            LiveModeChanged
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
