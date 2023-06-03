﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Home.API.home.Models;

public partial class Device
{
    public int Id { get; set; }

    public string Guid { get; set; }

    public string Name { get; set; }

    public string Ip { get; set; }

    public string MacAddress { get; set; }

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

    public virtual ICollection<DeviceBios> DeviceBios { get; set; } = new List<DeviceBios>();

    public virtual ICollection<DeviceChange> DeviceChange { get; set; } = new List<DeviceChange>();

    public virtual ICollection<DeviceCommand> DeviceCommand { get; set; } = new List<DeviceCommand>();

    public virtual ICollection<DeviceDiskDrive> DeviceDiskDrive { get; set; } = new List<DeviceDiskDrive>();

    public virtual ICollection<DeviceGraphic> DeviceGraphic { get; set; } = new List<DeviceGraphic>();

    public virtual ICollection<DeviceLog> DeviceLog { get; set; } = new List<DeviceLog>();

    public virtual ICollection<DeviceMessage> DeviceMessage { get; set; } = new List<DeviceMessage>();

    public virtual ICollection<DeviceScreen> DeviceScreen { get; set; } = new List<DeviceScreen>();

    public virtual ICollection<DeviceScreenshot> DeviceScreenshot { get; set; } = new List<DeviceScreenshot>();

    public virtual DeviceType DeviceType { get; set; }

    public virtual DeviceUsage DeviceUsage { get; set; }

    public virtual ICollection<DeviceWarning> DeviceWarning { get; set; } = new List<DeviceWarning>();

    public virtual DeviceEnvironment Environment { get; set; }

    public virtual DeviceOstype OstypeNavigation { get; set; }
}