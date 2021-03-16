using Newtonsoft.Json;
using System;
#if !LEGACY
using System.Text.Json.Serialization;
#endif

namespace Home.Data.Com
{
    public class AckResult
    {
        [JsonProperty("ack")]
#if !LEGACY
        [JsonPropertyName("ack")]
#endif
        public Ack Result { get; set; }

        [JsonProperty("data")]
#if !LEGACY
        [JsonPropertyName("data")]
#endif
        public string JsonData { get; set; }

        [Flags]
        public enum Ack
        {
            OK = 1,
            Invalid = 2,
            ScreenshotRequired = 4,
            MessageRecieved = 8,
            CommandRecieved = 16
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
