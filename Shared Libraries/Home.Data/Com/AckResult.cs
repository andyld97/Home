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
