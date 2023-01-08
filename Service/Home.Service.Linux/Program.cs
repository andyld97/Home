using Home.Data;
using Home.Data.Com;
using Home.Data.Helper;
using Home.Model;
using Home.Service.Windows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        #region Private Members
        private static readonly Device currentDevice = new Device();
        private static readonly DateTime startTime = DateTime.Now;
        private static Home.Communication.API api;
        private static JObject jInfo = null;

        private static readonly string CONFIG_FILENAME = "config.json";
        private static readonly Timer ackTimer = new Timer();
        private static readonly object _lock = new object();
        private static bool isSendingAck = false;
        private static string NormalUser = string.Empty;
        #endregion

        #region Main

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls($"http://0.0.0.0:{Consts.API_PORT}");
        });

        public static void Main(string[] args)
        {
            try
            {
                // Debug LSHW JSON FILES:
#if DEBUG
                var device = new Device();
                ParseHardwareInfo(System.IO.File.ReadAllText(@"Test\test6.json"), device);
                int debug = 0;
#endif

                Thread apiThread = new Thread(new ParameterizedThreadStart((_) =>
                {
                    var args = Environment.GetCommandLineArgs();
                    CreateHostBuilder(args).Build().Run();
                }));
                apiThread.Start();

                Task task = MainAsync(args);
                task.Wait();

                // if (Console.KeyAvailable)
                // Console.ReadKey();
                // else
                // {
                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                }
                // }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exiting: " + e.ToString());

                if (e.InnerException != null)
                    Console.WriteLine($"Inner Exception: {e.InnerException.ToString()}");
            }
        }

        public static async Task MainAsync(string[] args)
        {
            // Read config 
            string configJson = System.IO.File.ReadAllText(CONFIG_FILENAME);

            // Strip comments
            configJson = Regex.Replace(configJson, @"/\*(.*?)\*/", string.Empty, RegexOptions.Singleline);
            jInfo = JsonConvert.DeserializeObject<JObject>(configJson);

            bool isSignedIn = jInfo["is_signed_in"].Value<bool>();
            string id = jInfo["id"].Value<string>();

            api = new Communication.API(jInfo["api"].ToString());

            currentDevice.ID = id;
            currentDevice.Location = jInfo["location"].ToString();
            currentDevice.DeviceGroup = jInfo["device_group"].ToString();
            NormalUser = jInfo["user"].ToString();
            currentDevice.OS = (OSType)jInfo["os"].Value<int>();
            currentDevice.Environment.OSName = currentDevice.OS.ToString();
            currentDevice.Type = (DeviceType)jInfo["type"].Value<int>();
            currentDevice.Environment.StartTimestamp = startTime;
            RefreshDeviceInfo();

            if (!isSignedIn)
            {
                // Generate id 
                id = Guid.NewGuid().ToString();
                jInfo["id"] = id;
                currentDevice.ID = id;

                // Sign in
                var result = await api.RegisterDeviceAsync(currentDevice);
                if (result)
                {
                    jInfo["is_signed_in"] = true;
                    isSignedIn = true;
                }

                try
                {
                    System.IO.File.WriteAllText($"{CONFIG_FILENAME}.bak", configJson);
                    System.IO.File.WriteAllText(CONFIG_FILENAME, JsonConvert.SerializeObject(jInfo));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save data: {ex.Message}");
                }
            } 

            if (isSignedIn)
            {
                // Start ack timer
                ackTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
                ackTimer.Elapsed += AckTimer_Elapsed;
                ackTimer.Start();

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

                        // sudo zenity --error --text="Test" --title="hi"
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

                        Console.WriteLine("Showing message " + shellScript);
                        Helper.ExecuteSystemCommand("sudo", $"-H -u {NormalUser} bash -c \"sh zenity.sh\"", async: true);
                        Console.WriteLine("Test");
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

        #endregion

        #region Create Screenshot
        public static async Task CreateScreenshot()
        {
            Console.WriteLine("Creating a screenshot ...");

            // 1) Create a screenshot (but ensure that this command will be executed as the normal user)
            Helper.ExecuteSystemCommand("sudo", $"-H -u {NormalUser} bash -c \"sh screenshot.sh\"");

            // 2) Post screenshot to the api
            if (System.IO.File.Exists("screenshot.png"))
            {
                try
                {
                    byte[] data = await System.IO.File.ReadAllBytesAsync("screenshot.png");
                    var screenshotResult = await api.SendScreenshotAsync(new Screenshot() { DeviceID = currentDevice.ID, Data = Convert.ToBase64String(data) });

                    if (!screenshotResult.Success)
                        Console.WriteLine(screenshotResult.ErrorMessage);
                    else
                        Console.WriteLine("Succsessfully uploaded screeenshot!");
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

            ReadMemoryAndCPULoad();
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

        public static void ReadMemoryAndCPULoad()
        {
            // execute "free" proc
            string ramInfo = Helper.ExecuteSystemCommand("sh", "hw.sh");

            if (!string.IsNullOrEmpty(ramInfo))
            {
                string[] entries = ramInfo.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                // entries[0] := TOTAL
                // entries[1] := FREE

                string kbTotal = entries[0].ToLower().Replace("kb", string.Empty).Trim();
                string kbFree = entries[1].ToLower().Replace("kb", string.Empty).Trim();
                string cpuUsage = entries[2].Trim();

                if (ulong.TryParse(kbTotal, out ulong total) && ulong.TryParse(kbFree, out ulong free))
                {
                    currentDevice.Environment.TotalRAM = Math.Round((total / 1024.0 / 1024.0), 2);

                    // 3GB used (75 %)
                    double totalInBytes = total * 1024.0;
                    double freeInBytes = free * 1024.0;

                    double used = totalInBytes - freeInBytes;

                    double percentage = Math.Round((used / totalInBytes) * 100);
                    double usedInGB = Math.Round(used / Math.Pow(1024, 3));

                    currentDevice.Environment.FreeRAM = $"{usedInGB} GB used ({percentage} %)";
                }

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

            // Check if json is valid
            if (!Helper.IsValidJson(json))
            {
                Console.WriteLine("Ensure that the newest lswh version is installed (https://packages.debian.org/jessie/utils/lshw). Because it seems that you're using a version with produces invalid json!");
                Environment.Exit(-1);
            }

            currentDevice.DiskDrives.Clear();

            try
            {
                JToken item = null;

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
                Console.Write($"Failed to parse hwinfo: {ex}");
                return false;
            }

            // If nothing was found return "/"- as a diskdrive!
            if (device.DiskDrives.Count == 0)
                device.DiskDrives.Add(new DiskDrive() { VolumeName = "/", DriveName = "/", DriveID = "linux_default_storage", PhysicalName = "linux_default_storage" });

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
                        string line = lines.Where(l => l.StartsWith(volumeName) || l.EndsWith(volumeName)).FirstOrDefault();

                        string[] lineValues = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                        // lineValues[0] := Dateisysstem (/dev/sda1)
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

            if (childClass == "bus" && string.IsNullOrEmpty(device.Environment.Motherboard))
            {
                string vendor = child.Value<string>("vendor");
                string product = child.Value<string>("product");

                if (vendor != null && product != null)
                    device.Environment.Motherboard = $"{vendor} {product}";
            }
            if (childClass == "memory")
                device.Environment.TotalRAM = child.Value<long>("size");
            else if (childClass == "processor" && string.IsNullOrEmpty(device.Environment.CPUName))
                device.Environment.CPUName = child.Value<string>("product");
            if (childClass == "display")
                device.Environment.GraphicCards = new System.Collections.ObjectModel.ObservableCollection<string> { child.Value<string>("product") };
            else if (childClass == "network" && string.IsNullOrEmpty(device.IP))
                device.IP = child.Value<JObject>("configuration").Value<string>("ip");
            else if (childClass == "disk" || childClass == "volume")
            {
                string product = child.Value<string>("product");
                string description = child.Value<string>("description");

                var childs = child.Value<JArray>("children");

                // Volumes contain the infos directly, so simulate the children array!
                if (childClass == "volume")
                    childs = new JArray() { child };

                if (childs != null)
                {
                    foreach (var volume in childs)
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
    }
}