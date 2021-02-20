using Fluent;
using Home.Controls;
using Home.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Home
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var devices = Device.GenerateSampleDevices().OrderBy(p => p.Status); // == Device.Status.Active);
            foreach (var device in devices)
                DeviceHolder.Items.Add(new DeviceItem(device));

            MessageBox.Show(JsonConvert.SerializeObject(devices));

            TextDeviceLog.Text =  "[19:00] Screenshot von Andy-PC empfangen!" + Environment.NewLine + Environment.NewLine +  "[18:54] Verbindung mit Andy-PC hergestellt ...";
        }   
    }
}
