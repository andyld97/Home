using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Home.Data.Remote
{
    public class RemoteDirectory
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("last_change")]
        public DateTime LastChange { get; set; }

        [JsonPropertyName("files")]
        public List<RemoteFile> Files { get; set; } = new List<RemoteFile>();

        [JsonPropertyName("directories")]
        public List<RemoteDirectory> Directories { get; set; } = new List<RemoteDirectory>();

        public RemoteDirectory(string path)
        {
            this.Path = path;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
