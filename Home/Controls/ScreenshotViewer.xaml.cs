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
        private bool isInLiveMode = false;
        private string lastDate = string.Empty;
        private Device lastSelectedDevice = null;

        public delegate void resizeHandler(bool isLittle);
        public event resizeHandler OnResize;

        public event EventHandler OnScreenShotAquired;

        public ScreenshotViewer()
        {
            InitializeComponent();
            SetLiveMode(false, force: true);
        }

        public void SetImageSource(ImageSource bi)
        {
            ImageViewer.SetImageSource(bi);
        }

        public void UpdateDate(string text)
        {
            lastDate = text;

            if (isInLiveMode)
                TextLive.Text = $"Live - {text}";
            else
                TextLive.Text = $"{text}";
        }

        public void UpdateDevice(Device device)
        {
            if (device == null)
                return;

            lastSelectedDevice = device;

            SetLiveMode(false, true);
        }

        public void SetLiveMode(bool live, bool force = false)
        {
            if (lastSelectedDevice == null && !force)
                return;

            if (lastSelectedDevice?.Status == Device.DeviceStatus.Offline && !force)
            {
                MessageBox.Show("Das Gerät ist offline - wechseln in den Live Modus nicht möglich!");
                return;
            }

            MainBorder.BorderBrush =
            SubBorder.BorderBrush = (live ? new SolidColorBrush(Colors.Red) : FindResource("BlackBrush") as SolidColorBrush);            

            string path = $"pack://application:,,,/Home;Component/resources/icons/live/{(live ? "toggle" : "offline")}.png";
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bi.UriSource = new Uri(path);
            bi.EndInit();       

            ImageLive.Source = bi;

            UpdateLiveButton(live, lastSelectedDevice?.Status != Device.DeviceStatus.Offline);

            if (live)
            {
                TextLive.Foreground = new SolidColorBrush(Colors.Red);
                TextLive.Text = $"Live - {lastDate}";
            }
            else
            {
                TextLive.Foreground = FindResource("BlackBrush") as SolidColorBrush;
                TextLive.Text = lastDate;
            }
            

            isInLiveMode = live;
        }

        private void UpdateLiveButton(bool state, bool enabled)
        {
            ButtonToggleLiveMode.IsEnabled = enabled;
            string image = string.Empty;
            if (enabled)
                image = (!state ? "toggle" : "offline");
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

                    SaveFileDialog sfd = new SaveFileDialog { Filter = "Png Image (*.png)|*.png" };
                    var result = sfd.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        using (System.IO.FileStream stream = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create))
                            encoder.Save(stream);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Speichern des Bildes: {ex.Message}");
                }
            }
        }

        private void ButtonToggleLiveMode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SetLiveMode(!isInLiveMode);
            }
        }
    }
}
