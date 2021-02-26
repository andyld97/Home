using Home.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Home.Service.Linux
{
    class Program
    {
        static void Main(string[] args)
        {

            // test.json = "lshw -json" command under linux

            string content = System.IO.File.ReadAllText("test3.json"); // test.json
            JToken item = null;

            if (content.StartsWith("["))
            {
                var value = JsonConvert.DeserializeObject<JArray>(content);
                 item = value[0]; // class = system
            }
            else 
                item =  JsonConvert.DeserializeObject<JObject>(content);

            Device d = new Device();
            d.Name =
            d.Envoirnment.MachineName = item.Value<string>("id").ToUpper();
            d.Envoirnment.CPUCount = Environment.ProcessorCount;
            d.Envoirnment.Is64BitOS = Environment.Is64BitOperatingSystem;
            d.Envoirnment.UserName = Environment.UserName;
            d.Envoirnment.DomainName = Environment.UserDomainName;


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

                ProcessJToken(child, d);
            }

            int debug = 0;
        }


        public static void ProcessJToken(JToken child, Device d)
        {
            if (child == null || d == null)
                return;

            string childClass = child.Value<string>("class");
            string childID = child.Value<string>("id");

            if (childClass == "memory")
                d.Envoirnment.TotalRAM = child.Value<long>("size");
            else if (childClass == "processor" && string.IsNullOrEmpty(d.Envoirnment.CPUName))
                d.Envoirnment.CPUName = child.Value<string>("product");
            if (childClass == "display")
                d.Envoirnment.Graphics = child.Value<string>("product");
            else if (childClass == "network" && string.IsNullOrEmpty(d.IP))
                d.IP = child.Value<JObject>("configuration").Value<string>("ip");
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
                            JArray logicalNames = volume.Value<JArray>("logicalname");
                            string fs = volumeConfig.Value<string>("filesystem");

                            dd.DiskInterface = childID;
                            dd.DiskModel = product;
                            dd.DiskName = product;
                            dd.MediaLoaded = volumeConfig.Value<string>("state") == "mounted";
                            dd.VolumeSerial = serial;
                            dd.VolumeName = volume.Value<string>("id");
                            dd.PhysicalName = volume.Value<string>("physid");
                            dd.FileSystem = fs;
                            dd.TotalSpace = size;
                            dd.VolumeName = string.Join(",", logicalNames);

                            d.DiskDrives.Add(dd);
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
                    dd.FileSystem = fs;
                    dd.TotalSpace = size;
                    dd.VolumeName = string.Join(",", logicalNames);

                    d.DiskDrives.Add(dd);
                }
            }
        }
    }
}
