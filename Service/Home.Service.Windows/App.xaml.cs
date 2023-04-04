using Home.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Home.Service.Windows
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public static bool IsConfigFlagSet { get; set; } = false;

        private static Thread thread;

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls($"http://0.0.0.0:{Consts.API_PORT}");
        });

        private void Application_Startup(object sender, StartupEventArgs e)
        {   
            if (Environment.GetCommandLineArgs().Length > 0) 
                IsConfigFlagSet = Environment.GetCommandLineArgs().Any(p => p.ToLower().Contains("/config"));

            thread = new Thread(new ParameterizedThreadStart((_) =>
            {
                var args = Environment.GetCommandLineArgs();
                CreateHostBuilder(args).Build().Run();
            }));

            if (!IsConfigFlagSet)
                thread.Start();
        }

        public static void StartThread()
        {
            thread.Start();
        }
    }
}