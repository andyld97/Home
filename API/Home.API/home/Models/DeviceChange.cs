﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Home.API.home.Models;

public partial class DeviceChange
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public DateTime Timestamp { get; set; }

    public string Description { get; set; }

    public virtual Device Device { get; set; }
}