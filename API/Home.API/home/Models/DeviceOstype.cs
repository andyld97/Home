﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Home.API.home.Models
{
    public partial class DeviceOstype
    {
        public DeviceOstype()
        {
            Device = new HashSet<Device>();
        }

        public int OstypeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual ICollection<Device> Device { get; set; }
    }
}