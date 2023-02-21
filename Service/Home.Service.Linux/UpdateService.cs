using Home.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Home.Service.Linux
{
    public static class UpdateService
    {
        private static readonly string VersionUrl = "https://ca-soft.net/home/client-versions.json";
        private static readonly string UpdateUrl = "https://code-a-software.net/home/content/content.php?product=linux";

        public static async Task<bool?> CheckForUpdatesAsync(DateTime lastUpdateCheck)
        {
            bool? result = null;

            try
            {
                if (lastUpdateCheck != DateTime.MinValue && lastUpdateCheck.AddDays(1) >= DateTime.Now)
                {
                    // This is to prevent, that if an update fails, then it would be executed still on each start
                    // With this lock the update can be exectued in now+1day
                    result = null;
                }
                else
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var versions = await client.GetAsync(VersionUrl);
                        versions.EnsureSuccessStatusCode();
                        string versionsJson = await versions.Content.ReadAsStringAsync();

                        var vObj = JObject.Parse(versionsJson);
                        var clientWindowsVersion = vObj["Home.Service.Linux"];

                        string version = clientWindowsVersion["version"].Value<string>();
                        decimal dotnetVersion = clientWindowsVersion["dotnetVersion"].Value<decimal>();

                        if (System.Environment.Version.Major != (int)dotnetVersion)
                            return false;

                        if (Version.Parse(version) > Version.Parse(Consts.HomeServiceLinuxClientVersion))
                            result = true;
                        else
                            result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Failed to search for updates: {ex.Message}");
                result = null;
            }

            return result;
        }

        public static bool UpdateServiceClient(string dotnetPath)
        {
            string directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var psi = new ProcessStartInfo()
            {
                FileName = dotnetPath,
                WorkingDirectory = directory,
                Arguments = $"{System.IO.Path.Combine(directory, "ClientUpdate.dll")} \"{UpdateUrl}\" \"{directory}\"",
            };

            var proc = Process.Start(psi);
            proc.WaitForExit();
            return false;
        }
    }
}