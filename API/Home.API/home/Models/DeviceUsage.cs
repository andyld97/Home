﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Home.API.home.Models;

public partial class DeviceUsage
{
    public int DeviceUsageId { get; set; }

    public string Cpu { get; set; }

    public string Ram { get; set; }

    public string Disk { get; set; }

    public string Battery { get; set; }

    public virtual ICollection<Device> Device { get; set; } = new List<Device>();
}