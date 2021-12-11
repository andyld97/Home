using Home.Data.Com;
using Home.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using static Home.Model.Device;

namespace Home.Service.Linux
{
    public class Program
    {
        private static readonly Device currentDevice = new Device();
        private static readonly Version ClientVersion = new Version(0, 0, 4);
        private static readonly DateTime startTime = DateTime.Now;
        private static Home.Communication.API api;
        private static JObject jInfo = null;

        private static readonly string CONFIG_FILENAME = "config.json";
        private static readonly Timer ackTimer = new Timer();
        private static readonly object _lock = new object();
        private static bool isSendingAck = false;
        private static string NormalUser = string.Empty;

        public static void Main(string[] args)
        {
            try
            {
                // Test: ParseHardwareInfo(System.IO.File.ReadAllText(@"Test\test3.json"), new Device());

                Task task = MainAsync(args);
                task.Wait();

             //   if (Console.KeyAvailable)
               //     Console.ReadKey();
                //else
                {
                    while (true)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exiting: " + e.Message);
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
            currentDevice.Envoirnment.OSName = currentDevice.OS.ToString();
            currentDevice.Type = (DeviceType)jInfo["type"].Value<int>();
            currentDevice.Envoirnment.StartTimestamp = startTime;
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
                        ExecuteSystemCommand("sudo", $"-H -u {NormalUser} bash -c \"sh zenity.sh\"", async: true);
                        Console.WriteLine("Test");
                    }
                    else if (ackResult.Result.Result.HasFlag(AckResult.Ack.CommandRecieved))
                    {
                        try
                        {
                            var command = JsonConvert.DeserializeObject<Command>(ackResult.Result.JsonData);
                            if (command != null)
                                ExecuteSystemCommand(command.Executable, command.Parameter);
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

        public static async Task CreateScreenshot()
        {
            Console.WriteLine("Creating a screenshot ...");

            // 1) Create a screenshot (but ensure that this command will be executed as the normal user)
            ExecuteSystemCommand("sudo", $"-H -u {NormalUser} bash -c \"sh screenshot.sh\"");

            // 2) Post screenshot to the api
            if (System.IO.File.Exists("screenshot.png"))
            {
                try
                {
                    byte[] data = await System.IO.File.ReadAllBytesAsync("screenshot.png");
                    var screenshotResult = await api.SendScreenshotAsync(new Screenshot() { ClientID = currentDevice.ID, Data = Convert.ToBase64String(data) });

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

        public static void RefreshDeviceInfo()
        {
            currentDevice.Envoirnment.OSVersion = Environment.OSVersion.ToString();
            currentDevice.Envoirnment.CPUCount = Environment.ProcessorCount;
            currentDevice.Envoirnment.Is64BitOS = Environment.Is64BitOperatingSystem;
            currentDevice.Envoirnment.UserName = Environment.UserName;
            currentDevice.Envoirnment.DomainName = Environment.UserDomainName;
            currentDevice.ServiceClientVersion = $"vLinux{ClientVersion.ToString(3)}";
            currentDevice.Envoirnment.RunningTime = DateTime.Now.Subtract(startTime);

            ParseHardwareInfo(ExecuteSystemCommand("lshw", "-json"), currentDevice);
            ReadMemoryAndCPULoad();
            ReadDiskUsage();
        }

        public static string ExecuteSystemCommand(string command, string parameter, bool async = false)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                Process proc = new Process { StartInfo = new ProcessStartInfo(command, parameter) { RedirectStandardOutput = !async } };
                proc.Start();
                
                while (!proc.StandardOutput.EndOfStream && !async)
                {
                    var line = proc.StandardOutput.ReadLine();
                    sb.Append(line);
                    sb.Append(Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return sb.ToString();
        }

        public static void ReadDiskUsage()
        {
            string usageInfo = ExecuteSystemCommand("iostat", "-dx");

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

            currentDevice.Envoirnment.DiskUsage = Math.Round((usageSum / (double)amountOfDevices), 2);           
        }


        public static void ReadMemoryAndCPULoad()
        {
            // execute "free" proc
            string ramInfo = ExecuteSystemCommand("sh", "hw.sh");

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
                    currentDevice.Envoirnment.TotalRAM = Math.Round((total / 1024.0 / 1024.0), 2);

                    // 3GB used (75 %)

                    double totalInBytes = total * 1024.0;
                    double freeInBytes = free * 1024.0;

                    double used = totalInBytes - freeInBytes;

                    double percentage = Math.Round((used / totalInBytes) * 100);
                    double usedInGB = Math.Round(used / Math.Pow(1024, 3));

                    currentDevice.Envoirnment.FreeRAM = $"{usedInGB} GB used ({percentage} %)";
                }

                if (double.TryParse(cpuUsage, out double usage))
                    currentDevice.Envoirnment.CPUUsage = usage;
            }
        }

        private static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static void ParseHardwareInfo(string json, Device device)
        {
            if (string.IsNullOrEmpty(json))
                return;
             
            // Check if json is valid
            if (!IsValidJson(json))
            {
                Console.WriteLine("Ensure that the newest lswh version is installed (https://packages.debian.org/jessie/utils/lshw). Because it seems that you're using a version with produces invalid json!");
                Environment.Exit(-1);
            }

            currentDevice.DiskDrives.Clear();
            JToken item = null;

            if (json.StartsWith("["))
            {
                var value = JsonConvert.DeserializeObject<JArray>(json);
                item = value[0]; // class = system
            }
            else
                item = JsonConvert.DeserializeObject<JObject>(json);


            device.Name =
            device.Envoirnment.MachineName = item.Value<string>("id").ToUpper();

            if (item.Value<string>("product") != null && !item.Value<string>("product").Contains("To Be Filled By O.E.M."))
                device.Envoirnment.Product = item.Value<string>("product");

            if (item.Value<string>("description") != null)
                device.Envoirnment.Description = item.Value<string>("description");

            Queue<JToken> childrenQueue = new Queue<JToken>();
            childrenQueue.Enqueue(item);

            while (childrenQueue.Count > 0)
            {
                var child = childrenQueue.Dequeue();

                var subChilds = child.Value<JArray>("children");
                if (subChilds != null && subChilds.Count > 0)
                {
                    foreach (var it in subChilds)
                        childrenQueue.Enqueue(it);
                }

                ProcessJToken(child, device);
            }


            // Get df / try to fill missing values
            string result = ExecuteSystemCommand("df", "-H");

            if (!string.IsNullOrEmpty(result))
            {
                string[] lines = result.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var drive in device.DiskDrives)
                {
                    string volumeName = drive.VolumeName;
                    if (volumeName.Contains(","))
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

                        drive.TotalSpace = ParseDFEntry(lineValues[1]);
                        drive.FreeSpace = ParseDFEntry(lineValues[3]);
                    }
                }
            }
        }

        public static ulong ParseDFEntry(string entry)
        {
            // entry might be 3,3T, 449G, 500M or 123k
            string value = entry.Substring(0, entry.Length - 1);
            int factor = 0;

            if (entry.EndsWith("k"))
                factor = 1;
            else if (entry.EndsWith("M"))
                factor = 2;
            else if (entry.EndsWith("G"))
                factor = 3;
            else if (entry.EndsWith("T"))
                factor = 4;

            if (double.TryParse(value, out double entryValue))
                return (ulong)Math.Round(entryValue * Math.Pow(1024, factor));

            return 0;
        }


        public static void ProcessJToken(JToken child, Device device)
        {
            if (child == null || device == null)
                return;

            string childClass = child.Value<string>("class");
            string childID = child.Value<string>("id");

            if (childClass == "bus" && string.IsNullOrEmpty(device.Envoirnment.Motherboard))
            {
                string vendor = child.Value<string>("vendor");
                string product = child.Value<string>("product");

                if (vendor != null && product != null)
                    device.Envoirnment.Motherboard = $"{vendor} {product}";
            }
            if (childClass == "memory")
                device.Envoirnment.TotalRAM = child.Value<long>("size");
            else if (childClass == "processor" && string.IsNullOrEmpty(device.Envoirnment.CPUName))
                device.Envoirnment.CPUName = child.Value<string>("product");
            if (childClass == "display")
                device.Envoirnment.Graphics = child.Value<string>("product");
            else if (childClass == "network" && string.IsNullOrEmpty(device.IP))
                device.IP = child.Value<JObject>("configuration").Value<string>("ip");
            else if (childClass == "storage" && childID != "storage")
            {
                string product = child.Value<string>("product");
                string logicalName = child.Value<string>("logicalname");
                string serial = child.Value<string>("serial");

                if (logicalName != null) // := /dev/sda1 oterhwise this a controller
                {
                    var namespaceChild = child.Value<JArray>("children").FirstOrDefault();
                    if (namespaceChild != null)
                    {
                        var volumeChild = namespaceChild.Value<JArray>("children");
                        // volumes
                        if (volumeChild == null)
                            return;

                        foreach (var volume in volumeChild)
                        {
                            DiskDrive dd = new DiskDrive();
                            JObject volumeConfig = volume.Value<JObject>("configuration");

                            ulong size = volume.Value<ulong>("size");

                            string logicalNames = string.Empty;

                            try
                            {
                                logicalNames = string.Join(",", volume.Value<JArray>("logicalname"));
                            }
                            catch
                            {
                                try
                                {
                                    logicalName = volume.Value<string>("logicalname");
                                }
                                catch
                                {

                                }
                            }

                            if (string.IsNullOrEmpty(logicalNames))
                                logicalNames = logicalName;

                            if (string.IsNullOrEmpty(logicalNames))
                                logicalNames = "Unknown";
         
                            string fs = volumeConfig?.Value<string>("filesystem");

                            dd.DiskInterface = childID;
                            dd.DiskModel = product;
                            dd.DiskName = product;
                            try
                            {
                                dd.MediaLoaded = volumeConfig?.Value<string>("state") == "mounted";
                            }
                            catch
                            {

                            }
                            dd.VolumeSerial = serial;

                            try
                            {
                                dd.VolumeName = volume.Value<string>("id");
                            }
                            catch
                            {

                            }

                            try
                            {
                                dd.PhysicalName = volume.Value<string>("physid");
                            }
                            catch
                            {

                            }
                            dd.FileSystem = fs?.ToUpper();
                            dd.TotalSpace = size;
                            dd.VolumeName = logicalNames;

                            device.DiskDrives.Add(dd);
                        }
                    }
                }
            }
            else if (childClass == "volume")
            {
                string product = child.Value<string>("product");
                if (!string.IsNullOrEmpty(product))
                {
                    DiskDrive dd = new DiskDrive();

                    JObject volumeConfig = child.Value<JObject>("configuration");

                    ulong size = child.Value<ulong>("size");
                    JArray logicalNames = child.Value<JArray>("logicalname");
                    string fs = child.Value<string>("filesystem");

                    dd.DiskInterface = childID;
                    dd.DiskModel = product;
                    dd.DiskName = product;
                    dd.MediaLoaded = volumeConfig.Value<string>("state") == "mounted";
                    dd.VolumeSerial = child.Value<string>("serial");
                    dd.VolumeName = child.Value<string>("id");
                    dd.PhysicalName = child.Value<string>("physid");
                    dd.FileSystem = fs?.ToUpper();
                    dd.TotalSpace = size;
                    if (logicalNames != null)
                        dd.VolumeName = string.Join(",", logicalNames);         

                    device.DiskDrives.Add(dd);
                }
            }
        }
    }
}