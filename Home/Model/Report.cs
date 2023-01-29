using Home.Data.Helper;
using Home.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    public class Report
    {
        /// <summary>
        /// Generates a html report for the given device based on templates (can be found in resources)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static string GenerateHtmlDeviceReport(Device device, string dateTimeFormat)
        {
            string htmlTemplate = GetHtmlTemplate("template");

            string f(string value = "")
            {
                if (string.IsNullOrEmpty(value))
                    return "-";

                return value;
            }

            string runningTime = string.Empty;
            if (device.Environment.RunningTime.Days > 0)
                runningTime += $"{device.Environment.RunningTime.Days} {Home.Properties.Resources.strDays}";
            if (device.Environment.RunningTime.Hours > 0)
            {
                if (!string.IsNullOrEmpty(runningTime))
                    runningTime += ", ";

                runningTime += $"{device.Environment.RunningTime.Hours} {Home.Properties.Resources.strHours}";
            }
            if (device.Environment.RunningTime.Minutes > 0)
            {
                if (!string.IsNullOrEmpty(runningTime))
                    runningTime += ", ";

                runningTime += $"{device.Environment.RunningTime.Minutes} {Home.Properties.Resources.strMinutes}";
            }

            // Replace values
            // Name, OEM and additional infos
            htmlTemplate = htmlTemplate.Replace("{name}", f(device.Name));
            htmlTemplate = htmlTemplate.Replace("{oem0}", f(device.ID));
            htmlTemplate = htmlTemplate.Replace("{oem1}", f(device.Type.ToString()));
            htmlTemplate = htmlTemplate.Replace("{oem2}", f(device.Environment.Vendor));
            htmlTemplate = htmlTemplate.Replace("{oem3}", f(device.Environment.Product));
            htmlTemplate = htmlTemplate.Replace("{oem4}", f(device.Environment.Description));
            htmlTemplate = htmlTemplate.Replace("{oem5}", f(device.Environment.StartTimestamp.ToString(Home.Properties.Resources.strDateTimeFormat)));
            htmlTemplate = htmlTemplate.Replace("{oem6}", f(runningTime));
            htmlTemplate = htmlTemplate.Replace("{oem7}", f(device.Location));
            htmlTemplate = htmlTemplate.Replace("{oem8}", f(device.DeviceGroup));
            htmlTemplate = htmlTemplate.Replace("{oem9}", f(device.IP));
            htmlTemplate = htmlTemplate.Replace("{oem10}", f(device.MacAddress));
            htmlTemplate = htmlTemplate.Replace("{oem11}", f(device.ServiceClientVersion));

            // OS
            if (device.OS.IsWindows(true))
                htmlTemplate = htmlTemplate.Replace("{os0}", f($"{device.Environment.OSName} ({(device.Environment.Is64BitOS ? "64" : "32")} {Home.Properties.Resources.strBit})"));
            else
                htmlTemplate = htmlTemplate.Replace("{os0}", f($"{device.OS.GetDescription()} ({(device.Environment.Is64BitOS ? "64" : "32")} {Home.Properties.Resources.strBit})"));
            htmlTemplate = htmlTemplate.Replace("{os1}", f(device.Environment.OSVersion));
            htmlTemplate = htmlTemplate.Replace("{os2}", f(device.Environment.UserName));
            htmlTemplate = htmlTemplate.Replace("{os3}", f(device.Environment.DomainName));

            // Battery (b0..b3)
            htmlTemplate = htmlTemplate.Replace("{b0}", device.BatteryInfo != null ? Home.Properties.Resources.strYes : Home.Properties.Resources.strNo);
            htmlTemplate = htmlTemplate.Replace("{b1}", device.BatteryInfo != null ? device.BatteryInfo.BatteryLevelInPercent.ToString() : f());
            htmlTemplate = htmlTemplate.Replace("{b2}", device.BatteryInfo != null ? (device.BatteryInfo.IsCharging ? Home.Properties.Resources.strYes : Home.Properties.Resources.strNo) : f());

            // Hardware
            htmlTemplate = htmlTemplate.Replace("{h0}", f($"{device.Environment.CPUName} ({Home.Properties.Resources.strCores}: {device.Environment.CPUCount})"));
            htmlTemplate = htmlTemplate.Replace("{h1}", f(device.Environment.Motherboard));
            htmlTemplate = htmlTemplate.Replace("{h2}", f(ByteUnit.FromGB(Convert.ToUInt64(device.Environment.TotalRAM)).ToString()));
            htmlTemplate = htmlTemplate.Replace("{h3}", f(string.Join(Environment.NewLine, device.Environment.GraphicCards)));

            if (device.Screens.Count == 0)
                htmlTemplate = htmlTemplate.Replace("{screen}", string.Empty);
            else
            {
                string screenTemplate = string.Empty;
                screenTemplate = $"<h3>{Home.Properties.Resources.strScreens}</h3>";

                string tmp = GetHtmlTemplate("display_template");
                foreach (var screen in device.Screens)
                {
                    string subTemplate = tmp;

                    subTemplate = subTemplate.Replace("{dp0}", screen.ID);
                    subTemplate = subTemplate.Replace("{dp1}", screen.DeviceName);
                    subTemplate = subTemplate.Replace("{dp2}", screen.BuiltDate);
                    subTemplate = subTemplate.Replace("{dp3}", screen.Resolution);
                    subTemplate = subTemplate.Replace("{dp4}", screen.Index.ToString());
                    subTemplate = subTemplate.Replace("{dp5}", screen.IsPrimary ? Home.Properties.Resources.strYes : Home.Properties.Resources.strNo);

                    screenTemplate += $"{subTemplate}<hr />";
                }

                htmlTemplate = htmlTemplate.Replace("{screen}", screenTemplate);
            }

            // Warnings
            string warningsText = string.Empty;

            var warnings = GenerateWarningsReport(device, f);
            foreach (var warning in warnings)
                warningsText += warning + "<h2></h2>";

            if (string.IsNullOrEmpty(warningsText))
                warningsText = Home.Properties.Resources.strNoWarnings;
            htmlTemplate = htmlTemplate.Replace("{warnings}", warningsText);

            // log
            htmlTemplate = htmlTemplate.Replace("{log}", string.Join(Environment.NewLine, device.LogEntries.Select(p => p.ToString(dateTimeFormat))));

            string diskHtmlContent = string.Empty;
            foreach (var dd in device.DiskDrives)
                diskHtmlContent += GenerateDiskReport(dd, f) + "<h2></h2><p/>";

            htmlTemplate = htmlTemplate.Replace("{disks}", diskHtmlContent);

            // Replace placeholders
            htmlTemplate = htmlTemplate.Replace("{property}", Home.Properties.Resources.strProperty);
            htmlTemplate = htmlTemplate.Replace("{value}", Home.Properties.Resources.strValue);

            // Localizations

            // Categories
            htmlTemplate = htmlTemplate.Replace("{strOS}", Home.Properties.Resources.strReport_OS);
            htmlTemplate = htmlTemplate.Replace("{strHardware}", Home.Properties.Resources.strReport_Hardware);
            htmlTemplate = htmlTemplate.Replace("{strBattery}", Home.Properties.Resources.strReport_Battery);
            htmlTemplate = htmlTemplate.Replace("{strWarnings}", Home.Properties.Resources.strReport_Warnings);
            htmlTemplate = htmlTemplate.Replace("{strDeviceLog}", Home.Properties.Resources.strReport_DeviceLog);
            htmlTemplate = htmlTemplate.Replace("{strDisks}", Home.Properties.Resources.strReport_Disk);

            // OEM
            htmlTemplate = htmlTemplate.Replace("{strDeviceId}", Home.Properties.Resources.strReport_DeviceID);
            htmlTemplate = htmlTemplate.Replace("{strDeviceType}", Home.Properties.Resources.strReport_DeviceType);
            htmlTemplate = htmlTemplate.Replace("{strVendor}", Home.Properties.Resources.strReport_Vendor);
            htmlTemplate = htmlTemplate.Replace("{strProduct}", Home.Properties.Resources.strReport_Product);
            htmlTemplate = htmlTemplate.Replace("{strDescription}", Home.Properties.Resources.strReport_Description);
            htmlTemplate = htmlTemplate.Replace("{strStartTime}", Home.Properties.Resources.strReport_StartTimestamp);
            htmlTemplate = htmlTemplate.Replace("{strUptime}", Home.Properties.Resources.strReport_Uptime);
            htmlTemplate = htmlTemplate.Replace("{strLocation}", Home.Properties.Resources.strReport_Location);
            htmlTemplate = htmlTemplate.Replace("{strGroup}", Home.Properties.Resources.strReport_Group);
            htmlTemplate = htmlTemplate.Replace("{strIPAdress}", Home.Properties.Resources.strReport_IPAddress);
            htmlTemplate = htmlTemplate.Replace("{strMacAddress}", Home.Properties.Resources.strReport_MacAddress);
            htmlTemplate = htmlTemplate.Replace("{strHomeClientVersion}", Home.Properties.Resources.strReport_HomeClientVersion);

            // OS
            htmlTemplate = htmlTemplate.Replace("{strOsName}", Home.Properties.Resources.strReport_OsName);
            htmlTemplate = htmlTemplate.Replace("{strOsVersion}", Home.Properties.Resources.strReport_Version);
            htmlTemplate = htmlTemplate.Replace("{strUser}", Home.Properties.Resources.strReport_User);
            htmlTemplate = htmlTemplate.Replace("{strDomain}", Home.Properties.Resources.strReport_Domain);

            // Hardware
            htmlTemplate = htmlTemplate.Replace("{strCPU}", Home.Properties.Resources.strReport_CPU);
            htmlTemplate = htmlTemplate.Replace("{strMotherboard}", Home.Properties.Resources.strReport_Motherboard);
            htmlTemplate = htmlTemplate.Replace("{strRAM}", Home.Properties.Resources.strReport_RAM);
            htmlTemplate = htmlTemplate.Replace("{strGraphics}", Home.Properties.Resources.strReport_Graphics);

            // Battery
            htmlTemplate = htmlTemplate.Replace("{strBatteryExists}", Home.Properties.Resources.strReport_BatteryExists);
            htmlTemplate = htmlTemplate.Replace("{strBatteryRemaningPercentage}", Home.Properties.Resources.strReport_BatteryRemaningPercentage);
            htmlTemplate = htmlTemplate.Replace("{strBatteryIsCharging}", Home.Properties.Resources.strReport_IsCharging);

            // Disks
            htmlTemplate = htmlTemplate.Replace("{strDiskName}", Home.Properties.Resources.strReport_DiskDeviceName);
            htmlTemplate = htmlTemplate.Replace("{strDiskVolume}", Home.Properties.Resources.strReport_DiskVolumeName);
            htmlTemplate = htmlTemplate.Replace("{strDiskFilesystem}", Home.Properties.Resources.strReport_DiskFilesystem);
            htmlTemplate = htmlTemplate.Replace("{strDiskStorage}", Home.Properties.Resources.strReport_DiskStorage);
            htmlTemplate = htmlTemplate.Replace("{strDiskFreeStorage}", Home.Properties.Resources.strReport_DiskFreeSpace);

            // Warnings
            htmlTemplate = htmlTemplate.Replace("{strWarningType}", Home.Properties.Resources.strReport_WarningType);
            htmlTemplate = htmlTemplate.Replace("{strWarningMessage}", Home.Properties.Resources.strReport_WarningMessage);
            htmlTemplate = htmlTemplate.Replace("{strWarningOccuredDate}", Home.Properties.Resources.strReport_WarningOccured);

            // Display
            htmlTemplate = htmlTemplate.Replace("{strBuiltDate}", Home.Properties.Resources.strReport_BuiltDate);
            htmlTemplate = htmlTemplate.Replace("{strResolution}", Home.Properties.Resources.strReport_Resolution);
            htmlTemplate = htmlTemplate.Replace("{strIsPrimary}", Home.Properties.Resources.strReport_IsPrimary);

            return htmlTemplate;
        }

        private static string GenerateDiskReport(DiskDrive diskDrive, Func<string, string> f)
        {
            string htmlTemplate = GetHtmlTemplate("disk_template");
            htmlTemplate = htmlTemplate.Replace("{d0}", f(diskDrive.DiskModel));

            string volumes = diskDrive.VolumeName;
            if (volumes.Contains(','))
            {
                string[] temp = volumes.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                temp[0] += $" ({f(diskDrive.DriveID)})";

                volumes = string.Join("<br/>", temp);
            }
            else
                volumes += $" ({f(diskDrive.DriveID)})";

            htmlTemplate = htmlTemplate.Replace("{d1}", f(volumes));
            htmlTemplate = htmlTemplate.Replace("{d2}", f(diskDrive.FileSystem));

            double percentageUsed = Math.Round((Math.Abs(diskDrive.TotalSpace - (double)diskDrive.FreeSpace) / diskDrive.TotalSpace) * 100, 2);

            htmlTemplate = htmlTemplate.Replace("{d3}", f($"<b>{ByteUnit.FindUnit(diskDrive.TotalSpace)}</b> (Used: {percentageUsed} %)"));
            htmlTemplate = htmlTemplate.Replace("{d4}", f(ByteUnit.FindUnit(diskDrive.FreeSpace).ToString()));

            return htmlTemplate;
        }

        private static string GenerateWarningReport<T>(Warning<T> warning, Func<string, string> f)
        {
            string htmlTemplate = GetHtmlTemplate("warning_template");
            htmlTemplate = htmlTemplate.Replace("{w0}", f(warning.GetType().Name));
            htmlTemplate = htmlTemplate.Replace("{w1}", f(warning.Text));
            htmlTemplate = htmlTemplate.Replace("{w2}", f(warning.WarningOccoured.ToString()));

            return htmlTemplate;
        }

        private static List<string> GenerateWarningsReport(Device device, Func<string, string> f)
        {
            List<string> warnings = new List<string>();

            if (device.BatteryWarning != null)
                warnings.Add(GenerateWarningReport(device.BatteryWarning, f));

            foreach (var item in device.StorageWarnings)
                warnings.Add(GenerateWarningReport(item, f));

            return warnings;
        }

        private static string GetHtmlTemplate(string fileName)
        {
            var assembly = typeof(Report).Assembly;
            var stream = assembly.GetManifestResourceStream($"Home.resources.report.{fileName}.html");

            string html = string.Empty;

            using (System.IO.StreamReader sr = new System.IO.StreamReader(stream))
            {
                html = sr.ReadToEnd();
            }

            return html;
        }
    }
}