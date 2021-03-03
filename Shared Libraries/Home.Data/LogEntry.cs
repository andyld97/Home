using Newtonsoft.Json;
using System;

namespace Home.Data
{
    public class LogEntry
    {
        [JsonProperty("timestamp")]
        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("level")]
        [System.Text.Json.Serialization.JsonPropertyName("level")]
        public LogLevel Level { get; set; }

        [JsonProperty("message")]
        [System.Text.Json.Serialization.JsonPropertyName("message")]
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