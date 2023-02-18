using System.Diagnostics;

namespace ClientUpdate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Please wait while Home.Service is updating ...");

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                ExecuteUpdateWindows(args);
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

        private static void Log(string message) 
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Executes the setup and then starts the app again
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static void ExecuteUpdateWindows(string[] args)
        {
            try
            {                
                if (args.Length < 2)
                {
                    Environment.Exit(-1);
                    return;
                }

                // Wait till app is closed
                Task.Delay(2).Wait();

                string setupUrl = args[0];
                string executable = args[1];

                Log("Starting setup ...");

                // Start setup (verysilent)
                Process.Start(new ProcessStartInfo()
                {
                    FileName = setupUrl,
                    Arguments = "/sp /verysilent /supressmsgboxes /lang=\"english\" /forcecloseapplications",
                });

                Log("Waiting for setup to finish ...");

                // Wait for 30 seconds
                Thread.Sleep((int)TimeSpan.FromSeconds(30).TotalMilliseconds);

                Log("Restarting Home.Service.Windows ...");

                // Start Home.Service.Windows.exe
                Process.Start(executable, string.Empty);

                // Wait 5s
                Task.Delay(5000).Wait();

                // Exit app
               Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log($"Error while updating occured: {ex.Message} ...");
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadLine();
            }
        }
    }
}