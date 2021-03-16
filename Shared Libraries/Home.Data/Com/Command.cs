using Newtonsoft.Json;
#if !LEGACY
using System.Text.Json.Serialization;
#endif

namespace Home.Data.Com
{
    public class Command
    {
        [JsonProperty("executable")]
#if !LEGACY
        [JsonPropertyName("executable")]
#endif
        public string Executable { get; set; }

        [JsonProperty("parameter")]
#if !LEGACY
        [JsonPropertyName("parameter")]
#endif
        public string Parameter { get; set; }

        [JsonProperty("id")]
#if !LEGACY
        [JsonPropertyName("id")]
#endif
        public string DeviceID { get; set; }

        public override string ToString()
        {
            return Executable + " " + Parameter;
        }
    }
}
