using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;

namespace Home.Measure.Windows.SMART
{
    public static class SmartWMI
    {
        public static JObject DetermineDiskSmartStatus(string pnpDeviceId)
        {
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\WMI");
            ObjectQuery query = new ObjectQuery(@"SELECT * FROM MSStorageDriver_FailurePredictStatus Where InstanceName like ""%" + pnpDeviceId.Replace("\\", "\\\\") + @"%""");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection queryCollection = searcher.Get();

            bool? driveIsOk = null;
            foreach (ManagementObject m in queryCollection)
            {
                driveIsOk = (bool)m.Properties["PredictFailure"].Value == false;
            }

            if (driveIsOk == null)
                return null;

            JObject result = new JObject();
            result["status"] = driveIsOk;

            var attributes = DetermineSmartAttributes(searcher, pnpDeviceId).OrderBy(a => a.Register);
            JArray attribs = new JArray();
            foreach (var attrib in attributes)
            {
                JObject attribValue = new JObject();

                attribValue.Add("register", attrib.Register);
                attribValue.Add("is_ok", attrib.IsOK);
                attribValue.Add("current_value", attrib.Current);
                attribValue.Add("treshold", attrib.Threshold);
                attribValue.Add("worst", attrib.Worst);
                attribValue.Add("data", attrib.Data);

                attribs.Add(attribValue);
            }

            result["attributes"] = attribs;
            return result;
        }

        private static List<SmartAttribute> DetermineSmartAttributes(ManagementObjectSearcher searcher, string pnpDeviceId)
        {
            searcher.Query = new ObjectQuery(@"Select * from MSStorageDriver_FailurePredictData Where InstanceName like ""%" + pnpDeviceId.Replace("\\", "\\\\") + @"%""");
            List<SmartAttribute> attributes = new List<SmartAttribute>();

            foreach (ManagementObject data in searcher.Get())
            {
                byte[] bytes = (byte[])data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 42; ++i)
                {
                    try
                    {
                        int id = bytes[i * 12 + 2];

                        // Least significant status byte, +3 most significant byte, but not used so ignored.
                        int flags = bytes[i * 12 + 4];

                        //bool advisory = (flags & 0x1) == 0x0;
                        bool failureImminent = (flags & 0x1) == 0x1;
                        //bool onlineDataCollection = (flags & 0x2) == 0x2;

                        int value = bytes[i * 12 + 5];
                        int worst = bytes[i * 12 + 6];
                        int vendordata = BitConverter.ToInt32(bytes, i * 12 + 7);
                        if (id == 0) continue;

                        if (SMART.SmartAttribute.SmartAttributes.ContainsKey((byte)id))
                        {
                            attributes.Add(new SmartAttribute()
                            {
                                Current = value,
                                Worst = worst,
                                Data = vendordata,
                                Register = (byte)id,
                                IsOK = !failureImminent
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // given key does not exist in attribute collection (attribute not in the dictionary of attributes)
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            return DetermineAndAppendTresholdValues(attributes, searcher, pnpDeviceId);
        }

        private static List<SmartAttribute> DetermineAndAppendTresholdValues(List<SmartAttribute> smartAttributes, ManagementObjectSearcher searcher, string pnpDeviceId)
        {
            searcher.Query = new ObjectQuery(@"Select * from MSStorageDriver_FailurePredictThresholds Where InstanceName like ""%" + pnpDeviceId.Replace("\\", "\\\\") + @"%""");
            foreach (ManagementObject data in searcher.Get())
            {
                byte[] bytes = (byte[])data.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 42; ++i)
                {
                    try
                    {
                        int id = bytes[i * 12 + 2];
                        int thresh = bytes[i * 12 + 3];
                        if (id == 0) continue;

                        var attr = smartAttributes.Where(p => p.Register == (byte)id).FirstOrDefault();
                        if (attr != null)
                            attr.Threshold = thresh;
                    }
                    catch (Exception ex)
                    {
                        // given key does not exist in attribute collection (attribute not in the dictionary of attributes)
                        Debug.WriteLine(ex.Message);
                    }
                }
            }

            return smartAttributes;
        }
    }
}