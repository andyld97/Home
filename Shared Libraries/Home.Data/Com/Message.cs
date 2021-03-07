using Newtonsoft.Json;
#if !LEGACY
using System.Text.Json.Serialization;
#endif

namespace Home.Data.Com
{
    public class Message
    {
        [JsonProperty("device_id")]
#if !LEGACY
        [JsonPropertyName("device_id")]
#endif
        public string DeviceID { get; set; }

        [JsonProperty("title")]
#if !LEGACY
        [JsonPropertyName("title")]
#endif
        public string Title { get; set; }

        [JsonProperty("content")]
#if !LEGACY
        [JsonPropertyName("content")]
#endif
        public string Content { get; set; }

        [JsonProperty("type")]
#if !LEGACY
        [JsonPropertyName("type")]
#endif
        public MessageImage Type { get; set; } = MessageImage.Information;

        public enum MessageImage
        {
            Information,
            Warning,
            Error
        }

        public Message()
        { }

        public Message(string content, string title, MessageImage type)
        {
            this.Title = title;
            this.Content = content;
            Type = type;
        }

        public override string ToString()
        {
            return $"[{Type} (\"{Title}\")]: \"{Content}\"";
        }
    }
}
