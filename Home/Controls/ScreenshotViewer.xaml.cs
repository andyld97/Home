using Home.Communication;
using Home.Data;
using Home.Model;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für ScreenshotViewer.xaml
    /// </summary>
    public partial class ScreenshotViewer : UserControl
    {
        private bool isLittle = true;
        private string lastDate = string.Empty;
        private Device lastSelectedDevice = null;

        public delegate void resizeHandler(bool isLittle);
        public event resizeHandler OnResize;

        public event EventHandler OnScreenShotAquired;

        public ScreenshotViewer()
        {
            InitializeComponent();
        }

        public void SetImageSource(ImageSource bi)
        {
            ImageViewer.SetImageSource(bi);
        }

        public void UpdateDate(string text)
        {
            lastDate = text;

            if (lastSelectedDevice.IsLive.HasValue && lastSelectedDevice.IsLive.Value)
                TextLive.Text = $"Live - {text}";
            else
                TextLive.Text = $"{text}";
        }

        public void UpdateDevice(Device device)
        {
            if (device == null)
                return;

            lastSelectedDevice = device;
            bool enabled = device.Status != Device.DeviceStatus.Offline;
            bool status = device.IsLive ?? false;

            UpdateLiveStatus(status, enabled);
        }

        private void UpdateToggleButton(bool state, bool enabled)
        {
            ButtonToggleLiveMode.IsEnabled = enabled;
            string image;
            if (enabled)
                image = !state ? "toggle" : "offline";
            else
                image = "offline";

            string path2 = $"pack://application:,,,/Home;Component/resources/icons/live/{image}.png";
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bi.UriSource = new Uri(path2);
            bi.EndInit();

            ImageToggleLive.Source = bi;
          
        }

        private void UpdateLiveImage(bool state, bool enabled)
        {
            string path = $"pack://application:,,,/Home;Component/resources/icons/live/{(state ? "toggle" : "offline")}.png";
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bi.UriSource = new Uri(path);
            bi.EndInit();

            ImageLive.Source = bi;
        }

        private void UpdateLiveStatus(bool state, bool enabled)
        {
            UpdateToggleButton(state, enabled);
            UpdateLiveImage(state, enabled);       

            if (state)
            {
                TextLive.Foreground = new SolidColorBrush(Colors.Red);
                TextLive.Text = $"Live - {lastDate}";
            }
            else
            {
                TextLive.Foreground = FindResource("BlackBrush") as SolidColorBrush;
                TextLive.Text = lastDate;
            }
        }

        private void ButtonResize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isLittle = !isLittle;
                OnResize?.Invoke(isLittle);
            }
        }

        private void ButtonRefresh_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                OnScreenShotAquired?.Invoke(sender, EventArgs.Empty);
        }

        private void ButtonSaveImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)ImageViewer.ImageDisplay.Source));

                    SaveFileDialog sfd = new SaveFileDialog { Filter = "Png Bild (*.png)|*.png" };
                    var result = sfd.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        using (System.IO.FileStream stream = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create))
                            encoder.Save(stream);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Speichern des Bildes: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private API api;
        private Client client;

        public void PassApiAndClient(API api, Client client)
        {
            this.api = api;
            this.client = client;
        }

        private async void ButtonToggleLiveMode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (lastSelectedDevice?.Status == Device.DeviceStatus.Offline)
                {
                    MessageBox.Show("Das Gerät ist offline - wechseln in den Live Modus nicht möglich!", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (lastSelectedDevice.IsLive.HasValue)
                    await api.SetLiveStatusAsync(client, lastSelectedDevice, !lastSelectedDevice.IsLive.Value);
                else
                    await api.SetLiveStatusAsync(client, lastSelectedDevice, true);
            }
        }
    }
}