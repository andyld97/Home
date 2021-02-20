using Fluent;
using Home.Controls;
using Home.Data;
using Home.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Home
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private readonly Client client = new Client() { IsRealClient = true }; // ToDo: Fixed but unique (just save and load)
        private readonly Home.Communication.API api = null;

        private readonly DispatcherTimer updateTimer = new DispatcherTimer();
        private bool isUpdating = false;
        private readonly object _lock = new object();
        private List<Device> deviceList = new List<Device>();
        private Device lastSelectedDevice = null;

        public MainWindow()
        {
            InitializeComponent();

            TextDeviceLog.Text = "[19:00] Screenshot von Andy-PC empfangen!" + Environment.NewLine + Environment.NewLine + "[18:54] Verbindung mit Andy-PC hergestellt ...";
            api = new Communication.API("http://localhost:5000");
        }

        private async void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Initalize();
        }


        public async Task Initalize()
        {
            var result = await api.LoginAsync(client);
            deviceList = result.Result;

            if (result.Success)
                RefreshDeviceHolder();
            else
                MessageBox.Show(result.ErrorMessage);

            updateTimer.Interval = TimeSpan.FromSeconds(5);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void RefreshDeviceHolder()
        {
            DeviceHolder.Items.Clear();
            
            foreach (var device in deviceList.OrderBy(p => p.Status))
                DeviceHolder.Items.Add(device);
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (isUpdating)
                    return;
                else
                    isUpdating = true;
            }

            // TODO:  Check for event queues ...
            var result = await api.UpdateAsync(client);
            if (result.Success && result.Result != null)
            {
                var device = result.Result;
                if (deviceList.Any(d => d.ID == device.DeviceID ))
                {
                    // Update 
                    var oldDevice = deviceList.Where(d => d.ID == device.DeviceID).FirstOrDefault();
                    if (oldDevice != null)
                    {
                        // deviceList[deviceList.IndexOf(oldDevice)] = device.EventData.EventDevice;
                        oldDevice.Update(device.EventData.EventDevice, device.EventData.EventDevice.LastSeen, device.EventData.EventDevice.Status, true);

                        if (lastSelectedDevice == oldDevice)
                        {
                           // lastSelectedDevice = device.EventData.EventDevice;
                            //RefreshSelectedItem();
                        }

                        RefreshSelectedItem();
                        RefreshDeviceHolder();
                    }
                }
                else
                {
                    // Add
                    deviceList.Add(device.EventData.EventDevice);
                    MessageBox.Show("New device added!");

                    RefreshDeviceHolder();
                }
            }

            lock (_lock)
            {
                isUpdating = false;
            }
        }

        private void DeviceHolder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DeviceHolder.SelectedItem is Device dev)
            {
                lastSelectedDevice = dev;
                RefreshSelectedItem();
            }
        }

        private void RefreshSelectedItem()
        {
            if (lastSelectedDevice == null)
                return;

            TextDeviceLog.Text = string.Join("\n", lastSelectedDevice.LogEntries);
            TextDeviceLog.ScrollToEnd();
            DeviceInfo.DataContext = null;
            DeviceInfo.DataContext = lastSelectedDevice;
        }
    }
}
