using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace Home.Data.Com
{
    public class AckResult
    {
        [JsonProperty("ack")]
        [JsonPropertyName("ack")]
        public Ack Result { get; set; }

        [JsonProperty("data")]
        [JsonPropertyName("data")]
        public string JsonData { get; set; }

        [Flags]
        public enum Ack
        {
            OK = 0x01,
            Invalid = 0x02,
            ScreenshotRequired = 0x04,
            MessageRecieved = 0x08,
            CommandRecieved = 0x0F
        }

        public AckResult()
        { }

        public AckResult(Ack type, string data = "")
        {
            Result = type;
            JsonData = data;
        }
    }
}
