using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Home.Service.Windows
{
    public static class SingleInstanceManager
    {
        private static Mutex AppMutex = new Mutex(false, "3F911615-3164-47A1-831E-8CA56B49C3C4");

        // Entry Point: https://stackoverflow.com/a/6156665/6237448
        // Mutex:       https://stackoverflow.com/a/19165/6237448
        [STAThread]
        public static void Main(string[] args)
        {
            // Check if mutex is acquired
            if (!AppMutex.WaitOne(TimeSpan.FromSeconds(1), false))
                Environment.Exit(-1);

            // Run app
            var app = new App();
            app.InitializeComponent();
            app.Run();

            // Release app mutex
            AppMutex.ReleaseMutex();
        }

        public static void Exit(int exitCode = 0)
        {
            AppMutex.ReleaseMutex();
            Environment.Exit(exitCode);
        }
    }
}