﻿using Fluent;
using Home.Controls;
using Home.Model;
using System;
using System.Collections.Generic;
using System.Linq;

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

            var devices = Device.GenerateSampleDevices().OrderBy(p => p.DeviceStatus); // == Device.Status.Active);
            foreach (var device in devices)
                DeviceHolder.Items.Add(new DeviceItem(device));

            TextDeviceLog.Text =  "[19:00] Screenshot von Andy-PC empfangen!" + Environment.NewLine + Environment.NewLine +  "[18:54] Verbindung mit Andy-PC hergestellt ...";
        }   
    }
}
