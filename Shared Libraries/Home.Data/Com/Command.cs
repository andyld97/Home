using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Home.Data.Com
{
    public class Command
    {
        [JsonProperty("executable")]
        [JsonPropertyName("executable")]
        public string Executable { get; set; }

        [JsonProperty("parameter")]
        [JsonPropertyName("parameter")]
        public string Parameter { get; set; }
    }
}
