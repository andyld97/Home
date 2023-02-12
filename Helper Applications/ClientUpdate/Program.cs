using System.Diagnostics;

namespace ClientUpdate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Please wait while Home.Service is installing the update ...");

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                ExecuteUpdateWindowsAsync(args).Wait();
            else
                ExecuteUpdateViaShellScript(args);
        }

        /// <summary>
        /// Executes the update shell script
        /// </summary>
        /// <param name="args">1. Argument is the download to the tar file<br/>2.Argument is the local path of Home.Service.Linux</param>
        private static void ExecuteUpdateViaShellScript(string[] args)
        {
            if (args.Length < 2)
            {
                Environment.Exit(-1);
                return;
            }

            string url = args[0];
            string path = args[1];

            string command = "sudo";
            string commandArgs = $"-H bash -c \"sh update.sh {url} '{path}'\"";

            Home.Service.Linux.Helper.ExecuteSystemCommand(command, commandArgs);
            Environment.Exit(0);
        }

        /// <summary>
        /// Executes the setup and then starts the app again
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static async Task ExecuteUpdateWindowsAsync(string[] args)
        {
            if (args.Length < 2)
            {
                Environment.Exit(-1);
                return;
            }

            // Wait till app is closed
            await Task.Delay(2);

            string setupUrl = args[0];
            string executable = args[1];

            // Start setup (verysilent)
            Process.Start(new ProcessStartInfo()
            {
                FileName = setupUrl,
                // Arguments = "/sp /verysilent /supressmsgboxes /lang=\"english\" /forcecloseapplications",
                Arguments = "/sp /verysilent /lang=\"english\" /log=\"D:\\log.txt\"",
            });

            // Wait for 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1));

            // Start Home.Service.Windows.exe
            Process.Start(executable, string.Empty);

            // Wait 5s
            await Task.Delay(5000);

            // Exit app
            Environment.Exit(0);
        }
    }
}