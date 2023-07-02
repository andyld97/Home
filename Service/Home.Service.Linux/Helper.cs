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
        public static string ExecuteSystemCommand(string command, string parameter, bool async = false, Dictionary<string, string> env = null)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var info = new ProcessStartInfo(command, parameter) { RedirectStandardOutput = !async };

                if (env != null)
                {
                    foreach (var item in env.Keys)
                        info.EnvironmentVariables.Add(item, env[item]);
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
                Console.WriteLine(ex.Message);
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
                catch (JsonReaderException jex)
                {
                    // Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) // some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
                return false;
        }
    }
}