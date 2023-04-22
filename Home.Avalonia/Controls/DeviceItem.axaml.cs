using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using Home.Model;
using Avalonia.Media.Imaging;

namespace Home.Avalonia.Controls
{
    public partial class DeviceItem : UserControl
    {
        public DeviceItem()
        {
            InitializeComponent();
        }
        public DeviceItem(Device device)
        {
            DataContext = device;

            InitializeComponent();

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            string imageFileName = device.DetermineDeviceImage().Replace("/", ".");

            var result = assets.Open(new Uri($"resm:Home.Avalonia.Assets.icons.devices.{imageFileName}"));
            ImageDeviceIcon.Source = new Bitmap(result);
        }
    }
}
