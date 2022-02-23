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

        [JsonIgnore]
#if !LEGACY
        [System.Text.Json.Serialization.JsonIgnore]
#endif
        public bool LogTelegram { get; set; }

        public enum LogLevel
        {
            Debug,
            Information,
            Warning,
            Error
        }

        public LogEntry()
        { }

        public LogEntry(DateTime timestamp, string message, LogLevel level, bool logTelegram = false)
        {
            Timestamp = timestamp;
            Message = message;
            Level = level;
            LogTelegram = logTelegram;
        }

        public override string ToString()
        {
            return $"[{Timestamp.ToShortDateString()} @ {Timestamp.ToShortTimeString()}]: {Message}";
        }
    }
}