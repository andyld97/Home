﻿using Home.Data;
using Home.Data.Com;
using Home.Data.Helper;
using Home.Model;
using Home.Service.Windows;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static Home.Model.Device;
using Timer = System.Timers.Timer;

namespace Home.Service.Linux
{
    public class Program
    {
        private static Mutex AppMutex = new Mutex(false, "3F911615-3164-47A1-831E-8CA56B49C3C4");

        #region Private Members
        private static readonly Device currentDevice = new Device();
        private static readonly DateTime startTime = DateTime.Now;
        private static Home.Communication.API api;
        private static JObject config = null;

        private static readonly string CONFIG_FILENAME = "config.json";
        private static readonly Timer ackTimer = new Timer();
        private static readonly Timer updateTimer = new Timer();
        private static readonly object _lock = new object();
        private static bool isSendingAck = false;
        private static string NormalUser = string.Empty;
        
        private static bool enableScreenshots = true;
        private static bool checkForUpdatesOnStart = true;
        private static bool useAutomaticUpdateTimer = false;
        private static bool enableRemoteFileAccess = true;
        private static int automaticTimerIntervalHours = 24;
        private static int xDisplayIndex = 0;

        #endregion

        #region Main

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls($"http://0.0.0.0:{Consts.API_PORT}");
        });

        public static void Main(string[] args)
        {
            // Check if mutex is acquired
            if (!AppMutex.WaitOne(TimeSpan.FromSeconds(1), false))
            {
                Trace.WriteLine("Home.Service.Linux is already started!");
                Environment.Exit(-1);
                return;
            }

            try
            {
                // Debug LSHW JSON FILES:
#if DEBUG
                var device = new Device();
                string test = "16338464 kB\r\n15475208 kB\r\n0.00";
                ReadMemoryAndCPULoad(test);
                ParseHardwareInfo(System.IO.File.ReadAllText(@"Test\test8.json"), device);
                int debug = 0;
#endif

                // Read config 
                string configJson = System.IO.File.ReadAllText(CONFIG_FILENAME);

                // Strip comments
                configJson = Regex.Replace(configJson, @"/\*(.*?)\*/", string.Empty, RegexOptions.Singleline);
                config = JsonConvert.DeserializeObject<JObject>(configJson);

                // Parse settings
                if (config.ContainsKey("enable_screenshots"))
                    enableScreenshots = config["enable_screenshots"].Value<bool>();

                if (config.ContainsKey("enable_remote_file_access"))
                    enableRemoteFileAccess = config["enable_remote_file_access"].Value<bool>();

                if (config.ContainsKey("check_for_updates_on_start"))
                    checkForUpdatesOnStart = config["check_for_updates_on_start"].Value<bool>();

                if (config.ContainsKey("use_automatic_update_timer"))
                    useAutomaticUpdateTimer = config["use_automatic_update_timer"].Value<bool>();

                if (config.ContainsKey("automatic_timer_interval_hours"))
                    automaticTimerIntervalHours = config["automatic_timer_interval_hours"].Value<int>();

                if (config.ContainsKey("x_display_index"))
                    xDisplayIndex = config["x_display_index"].Value<int>();

                if (config.ContainsKey("ip"))
                    currentDevice.IP = config["ip"].Value<string>();

                if (config.ContainsKey("mac"))
                    currentDevice.MacAddress = config["mac"].Value<string>();

                if (checkForUpdatesOnStart && CheckAndExecuteUpdate())
                    return;

                Thread apiThread = new Thread(new ParameterizedThreadStart((_) =>
                {
                    var args = Environment.GetCommandLineArgs();
                    CreateHostBuilder(args).Build().Run();
                }));

                if (enableRemoteFileAccess)
                    apiThread.Start();

                MainAsync(args, configJson);

                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exiting: " + e.ToString());

                if (e.InnerException != null)
                    Trace.WriteLine($"Inner Exception: {e.InnerException.ToString()}");

                AppMutex.ReleaseMutex();
                return;
            }

#pragma warning disable CS0162 // Unerreichbarer Code wurde entdeckt.
            AppMutex.ReleaseMutex();
#pragma warning restore CS0162 // Unerreichbarer Code wurde entdeckt.
        }

        public static void MainAsync(string[] args, string configJson)
        {
            bool isSignedIn = config["is_signed_in"].Value<bool>();
            string id = config["id"].Value<string>();

            api = new Communication.API(config["api"].ToString());

            currentDevice.ID = id;
            currentDevice.Location = config["location"].ToString();
            currentDevice.DeviceGroup = config["device_group"].ToString();
            NormalUser = config["user"].ToString();
            currentDevice.OS = (OSType)config["os"].Value<int>();
            currentDevice.Environment.OSName = currentDevice.OS.ToString();
            currentDevice.Type = (DeviceType)config["type"].Value<int>();
            currentDevice.Environment.StartTimestamp = startTime;
            RefreshDeviceInfo();

            if (!isSignedIn)
            {
                // Generate id 
                id = Guid.NewGuid().ToString();
                config["id"] = id;
                currentDevice.ID = id;

                Console.WriteLine($"Using guid: {id} ...");

                // Sign in
                (bool, string) res = (false, string.Empty);
                var task = Task.Run(async () => res = await api.RegisterDeviceAsync(currentDevice));
                task.Wait();

                if (res.Item1)
                {
                    config["is_signed_in"] = true;
                    isSignedIn = true;
                }
                else
                {
                    Console.WriteLine($"Failed to register device: {res.Item2} ... Exiting ...");
                    AppMutex.ReleaseMutex();
                    Environment.Exit(-1);
                    return;
                }

                try
                {
                    System.IO.File.WriteAllText($"{CONFIG_FILENAME}.bak", configJson);
                    System.IO.File.WriteAllText(CONFIG_FILENAME, JsonConvert.SerializeObject(config));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failed to save data: {ex.Message}");
                }
            } 

            if (isSignedIn)
            {
                // Start ack timer
                ackTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
                ackTimer.Elapsed += AckTimer_Elapsed;
                ackTimer.Start();

                // Update timer
                if (useAutomaticUpdateTimer)
                {
                    updateTimer.Interval = TimeSpan.FromHours(automaticTimerIntervalHours).TotalMilliseconds;
                    updateTimer.Elapsed += UpdateTimer_Elapsed;
                    updateTimer.Start();
                }

                // Execute on start
                AckTimer_Elapsed(null, null);
            }
        }

        #endregion

        #region Ack

        private static async void AckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (isSendingAck)
                    return;
                else
                    isSendingAck = true;
            }

            try
            {
                Console.WriteLine("Sending ack ...");
                RefreshDeviceInfo();
                var ackResult = await api.SendAckAsync(currentDevice);
                // Process ack answer
                if (ackResult != null && ackResult.Success && ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.OK))
                {
                    if (ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.ScreenshotRequired))
                        await CreateScreenshot();

                    if (ackResult.Result.Result.HasFlag(Data.Com.AckResult.Ack.MessageRecieved))
                    {
                        // Show message
                        Message message = JsonConvert.DeserializeObject<Message>(ackResult.Result.JsonData);
                        string image = "info";
                        switch (message.Type)
                        {
                            case Message.MessageImage.Error: image = "error"; break;
                            case Message.MessageImage.Information: image = "info"; break;
                            case Message.MessageImage.Warning: image = "warning"; break;
                        }

                        // Usage: sudo zenity --error --text="Test" --title="hi"
                        string shellScript = $"#!bin/bash\nDISPLAY=:0 zenity --{image} --title=\"{message.Title}\" --text=\"{message.Content}\"";


                        try
                        {
                            System.IO.File.Delete("zenity.sh");
                        }
                        catch
                        { }

                        try
                        {
                            System.IO.File.WriteAllText("zenity.sh", shellScript);
                        }
                        catch
                        {

                        }

                        Console.WriteLine($"Showing message: {shellScript}");
                        Helper.ExecuteSystemCommand("sudo", $"-H -u {NormalUser} bash -c \"sh zenity.sh\"", async: true);
                    }
                    else if (ackResult.Result.Result.HasFlag(AckResult.Ack.CommandRecieved))
                    {
                        try
                        {
                            var command = JsonConvert.DeserializeObject<Command>(ackResult.Result.JsonData);
                            if (command != null)
                                Helper.ExecuteSystemCommand(command.Executable, command.Parameter);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            lock (_lock)
                isSendingAck = false;
        }

        private static void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckAndExecuteUpdate();
        }

        #endregion

        #region Create Screenshot
        public static async Task CreateScreenshot()
        {
            if (!enableScreenshots)
                return;

            string fileName = "screenshot.png";
            Console.WriteLine("Creating a screenshot ...");

            // 1) Delete old screenshot if it exists
            if (System.IO.File.Exists(fileName))
            {
                try
                {
                    System.IO.File.Delete(fileName);
                }
                catch { }
            }

            // 2) Create a screenshot (but ensure that this command will be executed as the normal user)
            Helper.ExecuteSystemCommand("sudo", $"-H -u {NormalUser} bash -c \"scrot {fileName}\"", false, new Dictionary<string, string>() { { "DISPLAY", $":{xDisplayIndex}" } });

            // 2) Post screenshot to the API
            if (System.IO.File.Exists(fileName))
            {
                try
                {
                    byte[] data = await System.IO.File.ReadAllBytesAsync(fileName);
                    var screenshotResult = await api.SendScreenshotAsync(new Screenshot() { DeviceID = currentDevice.ID, Data = Convert.ToBase64String(data) });

                    if (!screenshotResult.Success)
                        Console.WriteLine(screenshotResult.ErrorMessage);
                    else
                        Console.WriteLine("Successfully uploaded screenshot!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get screenshot: {ex.Message}");
                }
            }
        }
        #endregion

        #region Refresh / Read Device Stats

        public static void RefreshDeviceInfo()
        {
            currentDevice.Environment.OSVersion = Environment.OSVersion.ToString();
            currentDevice.Environment.CPUCount = Environment.ProcessorCount;
            currentDevice.Environment.Is64BitOS = Environment.Is64BitOperatingSystem;
            currentDevice.Environment.UserName = Environment.UserName;
            currentDevice.Environment.DomainName = Environment.UserDomainName;
            currentDevice.ServiceClientVersion = $"vLinux{Consts.HomeServiceLinuxClientVersion}";
            currentDevice.Environment.RunningTime = DateTime.Now.Subtract(startTime);

            bool result = ParseHardwareInfo(Helper.ExecuteSystemCommand("lshw", "-json"), currentDevice);
            if (!result)
                throw new Exception("No hardware info provided ... Exiting ...");

            ReadMemoryAndCPULoad(Helper.ExecuteSystemCommand("sh", "hw.sh"));
            ReadDiskUsage();
        }

        public static void ReadDiskUsage()
        {
            string usageInfo = Helper.ExecuteSystemCommand("iostat", "-dx");

            string[] lines = usageInfo.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            // Device            r/s     rkB/s   rrqm/s  %rrqm r_await rareq-sz     w/s     wkB/s   wrqm/s  %wrqm w_await wareq-sz     d/s     dkB/s   drqm/s  %drqm d_await dareq-sz  aqu-sz  %util

            double usageSum = 0.0;
            int amountOfDevices = 0;

            foreach (var line in lines)
            {
                var items = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var usageItem = items.LastOrDefault();

                if (double.TryParse(usageItem, out double currentUsage))
                {
                    usageSum += currentUsage;
                    amountOfDevices++;
                }
            }
            currentDevice.Environment.DiskUsage = Math.Round((usageSum / (double)amountOfDevices), 2);           
        }

        public static void ReadMemoryAndCPULoad(string ramInfo)
        {
            if (!string.IsNullOrEmpty(ramInfo))
            {
                string[] entries = ramInfo.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                // entries[0] := TOTAL
                // entries[1] := AVAIL
                // entries[2] := CPU

                string memTotal = entries[0].Trim();
                string memAvail = entries[1].Trim();
                string cpuUsage = entries[2].Trim();

                currentDevice.Environment.TotalRAM = GeneralHelper.ParseMemoryEntryInGB(memTotal);
                currentDevice.Environment.AvailableRAM = GeneralHelper.ParseMemoryEntryInGB(memAvail);

                if (double.TryParse(cpuUsage, out double usage))
                    currentDevice.Environment.CPUUsage = usage;
            }
        }
#endregion

        #region Parse Hardware Info

        public static bool ParseHardwareInfo(string json, Device device)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            // Check if JSON is valid
            if (!Helper.IsValidJson(json))
            {
                Console.WriteLine("Ensure that the newest lshw version is installed (https://packages.debian.org/jessie/utils/lshw). Because it seems that you're using a version with produces invalid json!");
                Environment.Exit(-1);
            }

            currentDevice.DiskDrives.Clear();

            try
            {
                JToken? item = null;

                if (json.StartsWith("["))
                {
                    var value = JsonConvert.DeserializeObject<JArray>(json);
                    item = value[0]; // class = system
                }
                else
                    item = JsonConvert.DeserializeObject<JObject>(json);


                device.Name =
                device.Environment.MachineName = item.Value<string>("id").ToUpper();

                if (item.Value<string>("product") != null && !item.Value<string>("product").Contains("To Be Filled By O.E.M."))
                    device.Environment.Product = item.Value<string>("product");

                if (item.Value<string>("description") != null)
                    device.Environment.Description = item.Value<string>("description");

                Queue<JToken> childrenQueue = new Queue<JToken>();
                childrenQueue.Enqueue(item);

                while (childrenQueue.Count > 0)
                {
                    var child = childrenQueue.Dequeue();
                    bool processButDoesntEnqueue = false;

                    // A Disk entry will be further processed in the ProcessJTokenMethod
                    if (child.Value<string>("class") == "disk")
                        processButDoesntEnqueue = true;

                    if (!processButDoesntEnqueue)
                    {
                        var subChilds = child.Value<JArray>("children");
                        if (subChilds != null && subChilds.Count > 0)
                        {
                            foreach (var it in subChilds)
                                childrenQueue.Enqueue(it);
                        }
                    }


                    ProcessJToken(child, device);
                }
            }
            catch (Exception ex)
            {
                Console.Write($"Failed to parse hw-info: {ex}");
                return false;
            }

            // If nothing was found return "/"- as a disk drive!
            if (device.DiskDrives.Count == 0)
                device.DiskDrives.Add(new DiskDrive() { VolumeName = "/", DriveName = "/", DriveID = "linux_default_storage", PhysicalName = "linux_default_storage", MediaType = device.Name }); ;

            // Get df / try to fill missing values
            string result = Helper.ExecuteSystemCommand("df", "-H");

            if (!string.IsNullOrEmpty(result))
            {
                string[] lines = result.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var drive in device.DiskDrives)
                {
                    string volumeName = drive.VolumeName;
                    if (volumeName.Contains(','))
                        volumeName = volumeName.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                    if (string.IsNullOrEmpty(volumeName))
                        continue;

                    if (lines.Any(l => l.StartsWith(volumeName) || l.EndsWith(volumeName)))
                    {
                        string line = lines.FirstOrDefault(l => l.StartsWith(volumeName) || l.EndsWith(volumeName));

                        string[] lineValues = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                        // lineValues[0] := Dateisystem (/dev/sda1)
                        // lineValues[1] := Größe (4,0T)
                        // lineValues[2] := Benutzt (3,3T)
                        // lineValues[3] := Verfügbar (449G)
                        // linesValue[4] := Verw% (82%)
                        // linesValue[5] := Eingehängt auf (/media/server/Server)

                        drive.TotalSpace = GeneralHelper.ParseDFEntry(lineValues[1]);
                        drive.FreeSpace = GeneralHelper.ParseDFEntry(lineValues[3]);
                    }
                }
            }

            return true;
        }

        public static void ProcessJToken(JToken child, Device device)
        {
            if (child == null || device == null)
                return;

            string childClass = child.Value<string>("class");
            string childID = child.Value<string>("id");

            if (childClass == "bus" && childID == "core" && string.IsNullOrEmpty(device.Environment.Motherboard))
            {
                string vendor = child.Value<string>("vendor");
                string product = child.Value<string>("product");

                if (!string.IsNullOrEmpty(vendor) && !string.IsNullOrEmpty(product))
                    device.Environment.Motherboard = $"{vendor} {product}";
                
                // otherwise empty => unknown
            }
            if (childClass == "memory" && childID == "firmware")
            {
                string date = child.Value<string?>("date");
                DateTime value = DateTime.MinValue;

                if (date != null && DateTime.TryParse(date, CultureInfo.InvariantCulture, out value))
                { }

                device.BIOS = new BIOS()
                {
                    Description = child.Value<string?>("description"),
                    ReleaseDate = value,
                    Vendor = child.Value<string?>("vendor"),
                    Version = child.Value<string?>("version"),
                };
            }
            if (childClass == "memory")
                device.Environment.TotalRAM = child.Value<long>("size");
            else if (childClass == "processor" && string.IsNullOrEmpty(device.Environment.CPUName))
                device.Environment.CPUName = child.Value<string>("product");
            if (childClass == "display")
                device.Environment.GraphicCards = new System.Collections.ObjectModel.ObservableCollection<string> { child.Value<string>("product") };
            else if (childClass == "network")
            { 
                if (string.IsNullOrEmpty(device.IP))
                    device.IP = child.Value<JObject>("configuration")?.Value<string>("ip");

                if (string.IsNullOrEmpty(device.MacAddress))
                    device.MacAddress = child.Value<string>("serial")?.ToUpper();
            }
            else if (childClass == "disk" || childClass == "volume")
            {
                string product = child.Value<string>("product");
                string description = child.Value<string>("description");

                var children = child.Value<JArray>("children");

                // Volumes contain the infos directly, so simulate the children array!
                if (childClass == "volume")
                    children = new JArray() { child };

                if (children != null)
                {
                    foreach (var volume in children)
                    {
                        JObject volumeConfig = volume.Value<JObject>("configuration");
                        string fs = volumeConfig?.Value<string>("filesystem") ?? string.Empty;

                        DiskDrive dd = new DiskDrive();
                        var logicalName = volume.Value<JToken>("logicalname");
                        if (logicalName is JArray arr)
                            dd.VolumeName = string.Join(",", arr);
                        else
                            dd.VolumeName = volume.Value<string>("logicalname");

                        dd.DriveID = dd.PhysicalName = volume.Value<string>("physid") ?? string.Empty;
                        dd.VolumeSerial = volume.Value<string>("serial");
                        dd.TotalSpace = volume.Value<ulong>("size");
                        dd.FileSystem = fs.ToUpper();
                        dd.DiskInterface = description;
                        dd.DiskModel = product;
                        dd.DiskName = product;
                        dd.MediaLoaded = (volumeConfig?.Value<string>("state") == "mounted");

                        // Some devices has the properties in the children array
                        var childToken = volume.Value<JToken>("children");
                        if (childToken != null && childToken is JArray volumeChilds)
                        {
                            var volumeChild = volumeChilds.FirstOrDefault();
                            if (volumeChild != null)
                            {
                                volumeConfig = volumeChild.Value<JObject>("configuration");
                                dd.FileSystem = volumeConfig.Value<string>("filesystem").ToUpper() ?? string.Empty;
                                dd.DriveName = volumeConfig.Value<string>("label") ?? dd.DriveName;

                                // LogicalNames
                                logicalName = volume.Value<JToken>("logicalname");
                                if (logicalName is JArray arm)
                                    dd.VolumeName = string.Join(",", arm);
                                else
                                    dd.VolumeName = volume.Value<string>("logicalname");

                                // Size
                                ulong temp = volume.Value<ulong>("size");
                                if (temp != 0)
                                    dd.TotalSpace = temp;

                                // Serial
                                dd.VolumeSerial = volumeConfig.Value<string>("serial") ?? dd.VolumeSerial;
                            }
                        }

                        // Only add if there is a valid volumeName to access it
                        if (!string.IsNullOrEmpty(dd.VolumeName))
                            device.DiskDrives.Add(dd);
                    }
                }
            }           
        }
        #endregion

        #region Update

        private static bool CheckAndExecuteUpdate()
        {
            var lastUpdateCheck = DateTime.MinValue;
            try
            {
                if (System.IO.File.Exists("update.txt"))
                    lastUpdateCheck = DateTime.Parse(System.IO.File.ReadAllText("update.txt"));
            }
            catch
            {
                // ignore
            }

            var result = Task.Run(async () => await UpdateService.CheckForUpdatesAsync(lastUpdateCheck)).Result;

            if (result != null)
            {
                try
                {
                    // Write last update datetime-stamp
                    System.IO.File.WriteAllText("update.txt", DateTime.Now.ToString("s"));
                }
                catch
                {
                    // ignore
                }
            }

            if (result.HasValue && result.Value)
            {
                string dotnetPath = config["dotnet_path"].Value<string>();

                if (UpdateService.UpdateServiceClient(dotnetPath))
                {
                    AppMutex.ReleaseMutex();
                    Environment.Exit(0);
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}