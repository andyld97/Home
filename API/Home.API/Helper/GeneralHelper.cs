using Home.API.home.Models;
using System;
using static Home.Model.Device;
using static Home.Data.Helper.GeneralHelper;

namespace Home.API.Helper
{
    public static class GeneralHelper
    {
        public static bool ConvertNullableValue(double? input, out int result)
        {
            result = default;

            if (input == null)
                return false;

            result = Convert.ToInt32(input.Value);
            return true;
        }

        public static (string, string) GetShutdownCommand(bool restart, OSType type)
        {
            if (type.IsLinux() || type == Home.Model.Device.OSType.Unix || type == Home.Model.Device.OSType.Other)
            {
                string executable = "shutdown";
                string parameter = "-h now";

                if (restart)
                {
                    executable = "reboot";
                    parameter = string.Empty;
                }

                return (executable, parameter);
            }
            else if (type.IsWindows(true))
            {
                string executable = "shutdown.exe";
                string parameter = $"/{(restart ? "r" : "s")} /f /t 00";

                return (executable, parameter);
            }

            return (string.Empty, string.Empty);
        }
    }
}