using Home.Model;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Home.Service
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Home.Communication.API api = null;
        private DateTime startTime = DateTime.Now;

        private Device currentDevice = null;
        private readonly DispatcherTimer ackTimer = new DispatcherTimer();
        private bool isInitalized = false;
        private bool isSendingAck = false;
        private object _lock = new object();

        public MainWindow()
        {
            InitializeComponent();

            api = new Communication.API("http://localhost:5000");
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            currentDevice = new Device()
            {
                IP = "192.168.178.39",
                DeviceGroup = "Hautprechner",
                Location = "Andys-Zimmer",
                Name = "Andy-PC",
                OS = Device.OSType.Windows10,
                Type = Device.DeviceType.Desktop,
                Envoirnment = new DeviceEnvironment()
                {
                    CPUCount = 16,
                    CPUName = "AMD Ryzen 7 2700X 4GHz",
                    TotalRAM = 24, //"24 GB",
                    OSName = "Micorosft Windows 10",
                    OSVersion = "NT 10.20H2.0.0",
                    RunningTime = now.Subtract(startTime)
                }
            };

            ackTimer.Tick += AckTimer_Tick;
            ackTimer.Interval = TimeSpan.FromMinutes(1);
            ackTimer.Start();
        }

        private async void AckTimer_Tick(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (isSendingAck)
                    return;
                else
                    isSendingAck = false;
            }

            if (!isInitalized)
            {
                // Initalize
                isInitalized = await api.RegisterDeviceAsync(currentDevice);
            }
            else
            {
                // Send ack

                // Update
                // currentDevice.CPUUsage 
                // currentDevice.FreeRAM
                var now = DateTime.Now;
                currentDevice.Envoirnment.RunningTime = now.Subtract(startTime);

                var result = await api.SendAckAsync(currentDevice);

            }


            lock (_lock)
            {
                isSendingAck = false;
            }
        }
    }
}
