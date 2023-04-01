using System;
using System.Runtime.InteropServices;

namespace Home.Measure.Windows
{
    /// <summary>
    /// Capsules all methods available via P/Invoke
    /// </summary>
    public static class Native
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer); //Used to use ref with comment below
                                                                                    // but ref doesn't work.(Use of [In, Out] instead of ref
                                                                                    //causes access violation exception on windows xp
                                                                                    //comment: most probably caused by MEMORYSTATUSEX being declared as a class
                                                                                    //(at least at pinvoke.net). On Win7, ref and struct work.

        // Alternate Version Using "ref," And Works With Alternate Code Below.
        // Also See Alternate Version Of [MEMORYSTATUSEX] Structure With
        // Fields Documented.
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "GlobalMemoryStatusEx", SetLastError = true)]
        static extern bool _GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        public static double DetermineTotalRAM()
        {
            try
            {
                GetPhysicallyInstalledSystemMemory(out long memKb);
                return Math.Round(memKb / 1024.0 / 1024.0, 2);
            }
            catch
            {
                //  Fail over (fall back)
                var msex = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(msex))
                    return Math.Round(msex.ullTotalPhys / Math.Pow(1024, 3), 2);
            }

            return 0;
        }
    }
}
