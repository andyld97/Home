using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Windows;

namespace Home.Service.Windows
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls("http://0.0.0.0:5556");
        });

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Thread thread = new Thread(new ParameterizedThreadStart((_) =>
            {
                var args = Environment.GetCommandLineArgs();
                CreateHostBuilder(args).Build().Run();
            }));

            thread.Start();
        }
    }
}