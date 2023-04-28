using Avalonia.Controls;
using Home.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Home.Model;
using Home.Avalonia.Controls;

namespace Home.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public static Client CLIENT = new Client() { IsRealClient = true };
        public static Home.Communication.API API = null;

        public MainWindow()
        {
            Initialized += MainWindow_Initialized;
            InitializeComponent();
        }

        private async void MainWindow_Initialized(object? sender, EventArgs e)
        {
            API = new Home.Communication.API("http://192.168.178.38:83"); // Settings.Instance.Host);
            CLIENT.ID = Guid.NewGuid().ToString();
            await Initalize();
        }

        private List<Device> deviceList = new List<Device>();


        public async Task Initalize()
        {
            var result = await API.LoginAsync(CLIENT);
            if (result.Result != null)
                deviceList = result.Result;

            if (result.Success)
                RefreshDeviceHolder();
            /*else
                MessageBox.Show(result.ErrorMessage, Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);*/

            /* updateTimer.Interval = TimeSpan.FromSeconds(5);
             updateTimer.Tick += UpdateTimer_Tick;
             updateTimer.Start();*/
        }

        private void RefreshDeviceHolder()
        {
            DeviceHolder.Children.Clear();


            foreach (var device in deviceList.Where(p => p.Status == Device.DeviceStatus.Active))
            {
                var di = new DeviceItem(device);
                di.OnDeviceItemSelected += Di_OnDeviceItemSelected;
                DeviceHolder.Children.Add(di);
            }

            foreach (var device in deviceList.Where(p => p.Status == Device.DeviceStatus.Offline))
            {
                var di = new DeviceItem(device);
                di.OnDeviceItemSelected += Di_OnDeviceItemSelected;
                DeviceHolderOffline.Children.Add(di);
            }

            var selectedDevice = deviceList.Where(p => p.Name.ToUpper() == "ANDY-PC").FirstOrDefault();
            RefreshSelectedDevice(selectedDevice);
        }

        public void RefreshSelectedDevice(Device selectedDevice)
        {
            DeviceInfo.Refresh(selectedDevice);
        }

        private void Di_OnDeviceItemSelected(DeviceItem deviceItem, Device device)
        {
            RefreshSelectedDevice(device);
        }

        /*private async void OnButtonClick(object sender, RoutedEventArgs e)
        {
            var mg = MessageBoxManager.GetMessageBoxStandardWindow("Hallo Welt!", TextName.Text, MessageBox.Avalonia.Enums.ButtonEnum.YesNoCancel);
           await mg.ShowDialog(this);

            Close();
        }*/
    }
}