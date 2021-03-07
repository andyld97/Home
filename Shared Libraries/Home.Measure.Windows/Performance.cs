using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Home.Measure.Windows
{
    /*
     * 
     * https://stackoverflow.com/a/41841500/6237448
     * 
     * 
     * 
     *  
     PerformanceCounter("Processor", "% Processor Time", "_Total");
     PerformanceCounter("Processor", "% Privileged Time", "_Total");
     PerformanceCounter("Processor", "% Interrupt Time", "_Total");
     PerformanceCounter("Processor", "% DPC Time", "_Total");
     PerformanceCounter("Memory", "Available MBytes", null);
     PerformanceCounter("Memory", "Committed Bytes", null);
     PerformanceCounter("Memory", "Commit Limit", null);
     PerformanceCounter("Memory", "% Committed Bytes In Use", null);
     PerformanceCounter("Memory", "Pool Paged Bytes", null);
     PerformanceCounter("Memory", "Pool Nonpaged Bytes", null);
     PerformanceCounter("Memory", "Cache Bytes", null);
     PerformanceCounter("Paging File", "% Usage", "_Total");
     PerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", "_Total");
     PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
     PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
     PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total");
     PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", "_Total");
     PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
     PerformanceCounter("Process", "Handle Count", "_Total");
     PerformanceCounter("Process", "Thread Count", "_Total");
     PerformanceCounter("System", "Context Switches/sec", null);
     PerformanceCounter("System", "System Calls/sec", null);
     PerformanceCounter("System", "Processor Queue Length", null);
 */

    /// <summary>
    /// Capsels all methods available by Performance Counter
    /// </summary>
    public static class Performance
    {
        private static readonly PerformanceCounter cpuCounter;
        private static readonly PerformanceCounter ramCounter;
        private static readonly PerformanceCounter diskCounter;

        static Performance()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        }

        public static double GetCPUUsage() => Math.Round(cpuCounter.NextValue(), 0);

        public static double GetDiskUsage() => Math.Round(diskCounter.NextValue(), 0);

        public static string DetermineFreeRAM()
        {
            long freeGB = (long)(ramCounter.NextValue() / 1024);
            double totalGB = Native.DetermineTotalRAM();
            double usedGB = totalGB - freeGB;

            int percentage = (int)Math.Round((usedGB / totalGB) * 100);

            return $"{usedGB} GB used ({percentage} %)";
        }

    }
}
