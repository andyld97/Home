using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Home.Service.Linux
{
    public static class Helper
    {
        public static string ExecuteSystemCommand(string command, string parameter, bool async = false, Dictionary<string, string> env = null!)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var info = new ProcessStartInfo(command, parameter) { RedirectStandardOutput = !async };

                if (env != null)
                {
                    foreach (var key in env.Keys)
                    {
                        Console.WriteLine($"Adding env: {key}={env[key]}");
                        info.EnvironmentVariables.Add(key, env[key]);
                    }
                }

                Process proc = new Process { StartInfo =  info };
                proc.Start();

                while (!proc.StandardOutput.EndOfStream && !async)
                {
                    var line = proc.StandardOutput.ReadLine();
                    sb.Append(line);
                    sb.Append(Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute system command: {ex}");
            }

            return sb.ToString();
        }

        public static bool IsValidJson(this string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) return false;
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) // For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException)
                {
                    // Exception in parsing json
                    return false;
                }
                catch (Exception)
                { 
                    return false;
                }
            }
            else
                return false;
        }
    }
}