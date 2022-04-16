using System;
using System.Text.Json.Serialization;

namespace Home.Data.Remote
{
    public class RemoteFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("length")]
        public long Length { get; set; }

        [JsonPropertyName("last_access_time")]
        public DateTime LastAccessTime { get; set; }

        [JsonPropertyName("last_write_time")]
        public DateTime LastWriteTime { get; set; }

        public RemoteFile(string path)
        {
            Path = path;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
