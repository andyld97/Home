using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Home.Data.Com
{
    public class Message
    {
        [JsonProperty("device_id")]
        [JsonPropertyName("device_id")]
        public string DeviceID { get; set; }

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("content")]
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonProperty("type")]
        [JsonPropertyName("type")]
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
