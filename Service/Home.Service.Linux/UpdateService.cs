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
        private static readonly string UpdateUrl = "https://ca-soft.net/home/content/content.php?product=linux";
        private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/109.0";

        private static string LastHash = string.Empty;

        public static async Task<bool?> CheckForUpdatesAsync(DateTime lastUpdateCheck)
        {
            bool? result = null;

            try
            {
                if (lastUpdateCheck != DateTime.MinValue && lastUpdateCheck.AddDays(1) >= DateTime.Now)
                {
                    // This is to prevent, that if an update fails, then it would be executed still on each start
                    // With this lock the update can be executed in now+1day
                    result = null;
                }
                else
                {
                    Console.WriteLine("Checking for updates ...");

                    using (HttpClient client = new HttpClient())
                    {
                        var versions = await client.GetAsync(VersionUrl);
                        versions.EnsureSuccessStatusCode();
                        string versionsJson = await versions.Content.ReadAsStringAsync();

                        var vObj = JObject.Parse(versionsJson);
                        var clientLinuxVersion = vObj["Home.Service.Linux"];

                        string version = clientLinuxVersion["version"].Value<string>();
                        decimal dotnetVersion = clientLinuxVersion["dotnetVersion"].Value<decimal>();
                        LastHash = clientLinuxVersion["fileHashSHA256"].Value<string>();

                        if (System.Environment.Version.Major != (int)dotnetVersion)
                            return false;

                        if (Version.Parse(version) > Version.Parse(Consts.HomeServiceLinuxClientVersion))
                        {
                            Console.WriteLine($"New update found: {version}");
                            result = true;
                        }
                        else
                        {
                            Console.WriteLine("No new update found...");
                            result = false;
                        }
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

        private static async Task<bool> DownloadFileAsync(string targetFilePath, string downloadLink)
        {
            try
            {
                Console.WriteLine($"Downloading update {downloadLink} ...");
                string dirName = System.IO.Path.GetDirectoryName(targetFilePath);
                if (!System.IO.Directory.Exists(dirName))
                    System.IO.Directory.CreateDirectory(dirName);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                    var stream = await client.GetStreamAsync(downloadLink);
                    using (FileStream fs = new FileStream(targetFilePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }
                }

                // Validate file / compare hash
                if (!string.IsNullOrEmpty(LastHash))
                {
                    // Build sha256-Hash
                    string fileHashSHA256 = await SHA256Hash.CreateHashFromFileAsync(targetFilePath);

                    if (fileHashSHA256 != LastHash)
                    {
                        try
                        {
                            // Delete corrupt/illegal file
                            System.IO.File.Delete(targetFilePath);
                        }
                        catch  { }

                        Console.WriteLine("Invalid hash found: Deleting file ...");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed while downloading update: {ex.Message}");
            }

            return false;
        }

        public static bool UpdateServiceClient(string dotnetPath)
        {
            string directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string updateFileName = "update.tar";
            string updateTarPath = System.IO.Path.Combine(directory, updateFileName);

            // Download and verify update
            bool result = false;
            Task.Run(async () => result = await DownloadFileAsync(updateTarPath, UpdateUrl)).Wait();

            if (result)
            {
                var psi = new ProcessStartInfo()
                {
                    FileName = dotnetPath,
                    WorkingDirectory = directory,
                    Arguments = $"{System.IO.Path.Combine(directory, "ClientUpdate.dll")} \"{updateFileName}\" \"{directory}\"",
                };

                var proc = Process.Start(psi);
                proc.WaitForExit();
                return true;
            }
            return false;
        }
    }
}