using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace Home.Data
{
    public class LogEntry
    {
        [JsonProperty("timestamp")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
#endif
        public DateTime Timestamp { get; set; }

        [JsonProperty("level")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("level")]
#endif
        public LogLevel Level { get; set; }

        [JsonProperty("message")]
#if !LEGACY
        [System.Text.Json.Serialization.JsonPropertyName("message")]
#endif
        public string Message { get; set; }

        [JsonIgnore]
#if !LEGACY
        [System.Text.Json.Serialization.JsonIgnore]
#endif
        [XmlIgnore]
        public bool NotifyWebHook { get; set; }

        public enum LogLevel
        {
            Debug,
            Information,
            Warning,
            Error
        }

        public LogEntry()
        { }

        public LogEntry(string message, LogLevel level, bool logTelegram = false) : this(DateTime.Now, message, level, logTelegram)
        { }

        public LogEntry(DateTime timestamp, string message, LogLevel level, bool notifyWebHook = false)
        {
            Timestamp = timestamp;
            Message = message;
            Level = level;
            NotifyWebHook = notifyWebHook;
        }

        public override string ToString()
        {
            return $"[{Timestamp.ToShortDateString()} @ {Timestamp.ToShortTimeString()}]: {Message}";
        }
    }
}