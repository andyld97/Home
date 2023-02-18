using Home.Service.Windows.Model;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Home.Service.Windows
{
    public class UpdateService
    {
        private static readonly string VersionUrl = "https://ca-soft.net/home/client-versions.json";
        private static readonly string GitHubReleaseUrl = "https://api.github.com/repos/andyld97/Home/releases/latest";
        private static readonly string AppExeName = "Home.Service.Windows.Setup.exe";
        private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/109.0";
        // private static readonly string LocalSetupFileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Home", "Home.Service.Windows.Setup.exe");
        private static readonly string LocalSetupFileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Home", "hc-setup.zip");

        public static async Task<bool?> CheckForUpdatesAsync()
        {
            bool? result = null;
            try
            {
                if (ServiceData.Instance.LastUpdateCheck != DateTime.MinValue && ServiceData.Instance.LastUpdateCheck.AddDays(1) >= DateTime.Now)
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
                        var clientWindowsVersion = vObj["Home.Service.Windows"];

                        string version = clientWindowsVersion["version"].Value<string>();
                        decimal dotnetVersion = clientWindowsVersion["dotnetVersion"].Value<decimal>();

                        if (System.Environment.Version.Major != (int)dotnetVersion)
                            return false;

                        if (Version.Parse(version) > typeof(UpdateService).Assembly.GetName().Version)
                        {
                            ServiceData.Instance.LastUpdateCheck = DateTime.Now;
                            result = true;
                        }
                        else
                        {
                            // Check if setup still exists, then clean up
                            if (System.IO.File.Exists(LocalSetupFileName))
                            {
                                try
                                {
                                    System.IO.File.Delete(LocalSetupFileName);
                                }
                                catch
                                {
                                    // ignore
                                }
                            }

                            ServiceData.Instance.LastUpdateCheck = DateTime.Now;
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

            ServiceData.Instance.Save();
            return result;
        }

        public static async Task<bool> UpdateServiceClient()
        {
            var downloadlink = await GetDownloadLinkAsync();           

            if (await DownloadFileAsync(LocalSetupFileName, downloadlink))
            {
                var now = DateTime.Now;
                string currentServiceExecutable = System.Reflection.Assembly.GetExecutingAssembly().Location;
                currentServiceExecutable = System.IO.Path.ChangeExtension(currentServiceExecutable, ".exe");

                try
                {
                    // Copy updater to a temp directory
                    var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Home");
                    if (!System.IO.Directory.Exists(tempPath))
                        System.IO.Directory.CreateDirectory(tempPath);

                    // Copy updater files to temp directoy to execute it from there (to prevent that it gets killed via Setup)
                    string[] filesToCopy = { "ClientUpdate.exe", "ClientUpdate.dll", "ClientUpdate.deps.json", "ClientUpdate.runtimeconfig.json", "Newtonsoft.Json.dll" };
                    foreach (var file in filesToCopy)
                    {
                        string srcPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentServiceExecutable), file);
                        string destPath = System.IO.Path.Combine(tempPath, file);
                        System.IO.File.Copy(srcPath, destPath, true);
                    }

                    string updaterLocation = System.IO.Path.Combine(tempPath, "ClientUpdate.exe");
                    string arguments = $"\"{LocalSetupFileName}\" \"{currentServiceExecutable}\"";

                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = updaterLocation,
                        Arguments = arguments
                    });

                    // Exit to ensure that the setup can run without any problems
                    SingleInstanceManager.Exit();
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to execute update: {ex.Message}");
                }
            }

            return false;
        }

        private static async Task<bool> DownloadFileAsync(string targetFilePath, string downloadLink)
        {
            try
            {
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
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed while downloading update: {ex.Message}");
            }

            return false;
        }

        private static async Task<string> GetDownloadLinkAsync()
        {
            return "https://code-a-software.net/home/content/hc-setup.zip";

            try
            {              
                using (HttpClient client = new HttpClient())
                {
                    // GitHub API needs a User Agent
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                    var result = await client.GetAsync(GitHubReleaseUrl);
                    string content = await result.Content.ReadAsStringAsync();
                    var info = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(content);

                    foreach (var asset in info["assets"].Value<JArray>())
                    {
                        if (asset["name"].Value<string>() == AppExeName)
                        {
                            string downloadUrl = asset["browser_download_url"].Value<string>();
                            return downloadUrl;
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                Trace.TraceError($"Failed to ask github API for getting the download link: {ex.Message}");
            }

            return string.Empty;
        }

        private static void ScheduleTask(string name, string description, string executable, string arguments, TimeSpan delay, DateTime endBoundary)
        {
            // Get the service on the local machine
            using (TaskService ts = new TaskService())
            {
                // Create a new task definition and assign properties
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = description;
                //td.Principal.RunLevel = TaskRunLevel.Highest;

                // Create a trigger that will fire only once (EndBoundary)
                td.Triggers.Add(new RegistrationTrigger() { Delay = delay, EndBoundary = endBoundary });

                // Create an action that will launch Notepad whenever the trigger fires
                td.Actions.Add(new ExecAction(executable, arguments, null));

                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(name, td);

                // Remove the task we just created
                // ts.RootFolder.DeleteTask("Test");
            }
        }
    }
}