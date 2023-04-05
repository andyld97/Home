using Home.Helper;
using Home.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Home.Converter
{
    public class ScreenshotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device d)
            {
                // Main screen or all screens
                var lastExistingScreenshot = d.Screenshots.OrderByDescending(p => p.Timestamp).FirstOrDefault(x => x.ScreenIndex == 0 || x.ScreenIndex == null)?.Filename;
                if (lastExistingScreenshot != null)
                {
                    string path = System.IO.Path.Combine(MainWindow.CACHE_PATH, d.ID, lastExistingScreenshot) + ".png";
                    if (!System.IO.File.Exists(path))
                        Task.Run(async () => await MainWindow.API.DownloadScreenshotToCache(d, MainWindow.CACHE_PATH, lastExistingScreenshot)).Wait();

                    return ImageHelper.LoadImage(path, true, d.Type == Device.DeviceType.Smartphone);
                }
                else return ImageHelper.LoadImage(string.Empty, true, d.Type == Device.DeviceType.Smartphone);
            }

            return ImageHelper.LoadImage(string.Empty, true, false);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}