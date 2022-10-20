﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Home.API.home.Models
{
    public partial class Device
    {
        public Device()
        {
            DeviceCommand = new HashSet<DeviceCommand>();
            DeviceDiskDrive = new HashSet<DeviceDiskDrive>();
            DeviceGraphic = new HashSet<DeviceGraphic>();
            DeviceLog = new HashSet<DeviceLog>();
            DeviceMessage = new HashSet<DeviceMessage>();
            DeviceScreenshot = new HashSet<DeviceScreenshot>();
        }

        public int Id { get; set; }
        public string Guid { get; set; }
        public string Name { get; set; }
        public string Ip { get; set; }
        public DateTime LastSeen { get; set; }
        public bool Status { get; set; }
        public int DeviceTypeId { get; set; }
        public int Ostype { get; set; }
        public string DeviceGroup { get; set; }
        public string Location { get; set; }
        public bool? IsLive { get; set; }
        public int EnvironmentId { get; set; }
        public string ServiceClientVersion { get; set; }
        public bool IsScreenshotRequired { get; set; }
        public int? DeviceUsageId { get; set; }

        public virtual DeviceType DeviceType { get; set; }
        public virtual DeviceUsage DeviceUsage { get; set; }
        public virtual DeviceEnvironment Environment { get; set; }
        public virtual DeviceOstype OstypeNavigation { get; set; }
        public virtual ICollection<DeviceCommand> DeviceCommand { get; set; }
        public virtual ICollection<DeviceDiskDrive> DeviceDiskDrive { get; set; }
        public virtual ICollection<DeviceGraphic> DeviceGraphic { get; set; }
        public virtual ICollection<DeviceLog> DeviceLog { get; set; }
        public virtual ICollection<DeviceMessage> DeviceMessage { get; set; }
        public virtual ICollection<DeviceScreenshot> DeviceScreenshot { get; set; }
    }
}