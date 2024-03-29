﻿using Home.API.home;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Home.API.Helper
{
    public static class DeviceHelper
    {
        public static async Task<home.Models.Device> GetDeviceByIdAsync(this HomeContext context, string guid)
        {
            return await context.Device.Include(d => d.DeviceLog)
                                       .Include(p => p.DeviceGraphic)
                                       .Include(p => p.Environment).ThenInclude(p => p.Battery)
                                       .Include(p => p.DeviceDiskDrive)
                                       .Include(p => p.DeviceUsage)
                                       .Include(p => p.DeviceCommand)
                                       .Include(p => p.DeviceMessage)
                                       .Include(p => p.DeviceWarning)
                                       .Include(p => p.DeviceScreen)
                                       .Include(p => p.DeviceChange)
                                       .Include(p => p.DeviceBios)
                                       .Include(p => p.DeviceScreenshot).ThenInclude(p => p.Screen) 
                                       .Include(p => p.OstypeNavigation).Where(p => p.Guid == guid).AsSplitQuery().FirstOrDefaultAsync();
        }

        public static async Task<List<home.Models.Device>> GetAllDevicesAsync(this HomeContext context, bool noTracking)
        {
            var devices = context.Device.Include(d => d.DeviceLog)
                                       .Include(p => p.DeviceGraphic)
                                       .Include(p => p.Environment).ThenInclude(p => p.Battery)
                                       .Include(p => p.DeviceDiskDrive)
                                       .Include(p => p.DeviceUsage)
                                       .Include(p => p.DeviceCommand)
                                       .Include(p => p.DeviceMessage)
                                       .Include(p => p.DeviceWarning)
                                       .Include(p => p.DeviceScreen)
                                       .Include(p => p.DeviceChange)
                                       .Include(p => p.DeviceBios)
                                       .Include(p => p.DeviceScreenshot).ThenInclude(p => p.Screen)
                                       .Include(p => p.OstypeNavigation).AsSplitQuery();

            if (noTracking)
                return await devices.AsNoTracking().ToListAsync();

            return await devices.ToListAsync();
        }

        public static async Task<IEnumerable<home.Models.Device>> GetInactiveDevicesAsync(this HomeContext context)
        {
            var list = await context.Device.Include(d => d.DeviceLog)
                                           .Include(p => p.DeviceGraphic)
                                           .Include(p => p.Environment).ThenInclude(p => p.Battery)
                                           .Include(p => p.DeviceDiskDrive)
                                           .Include(p => p.DeviceUsage)
                                           .Include(p => p.DeviceCommand)
                                           .Include(p => p.DeviceMessage)
                                           .Include(p => p.DeviceWarning)
                                           .Include(p => p.DeviceScreen)
                                           .Include(p => p.DeviceChange)
                                           .Include(p => p.DeviceBios)
                                           .Include(p => p.DeviceScreenshot).ThenInclude(p => p.Screen)
                                           .Include(p => p.OstypeNavigation).Where(d => d.Status).AsSplitQuery().ToListAsync();

            return list.Where(d => d.LastSeen.Add(Program.GlobalConfig.RemoveInactiveClients) < DateTime.Now);
        }
    }
}