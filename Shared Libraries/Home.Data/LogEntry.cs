using Newtonsoft.Json;
using System;

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

        public enum LogLevel
        {
            Debug,
            Information,
            Warning,
            Error
        }


        public LogEntry()
        { }

        public LogEntry(DateTime timestamp, string message, LogLevel level)
        {
            Timestamp = timestamp;
            Message = message;
            Level = level;
        }

        public override string ToString()
        {
            return $"[{Timestamp.ToShortDateString()} @ {Timestamp.ToShortTimeString()}]: {Message}";
        }
    }
}