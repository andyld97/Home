﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Home.API.home.Models;

public partial class DeviceScreen
{
    public int Id { get; set; }

    public string ScreenId { get; set; }

    public int DeviceId { get; set; }

    public string Manufacturer { get; set; }

    public string Serial { get; set; }

    public string BuiltDate { get; set; }

    public int ScreenIndex { get; set; }

    public bool IsPrimary { get; set; }

    public string DeviceName { get; set; }

    public string Resolution { get; set; }

    public virtual Device Device { get; set; }

    public virtual ICollection<DeviceScreenshot> DeviceScreenshot { get; set; } = new List<DeviceScreenshot>();
}