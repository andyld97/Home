using Home.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Home.Converter
{
    public class ScreenshotConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is List<string> screenshotFileNames && values[1] is string id)
            {
                var lastExistingScreenshot = screenshotFileNames?.LastOrDefault(s => System.IO.File.Exists(System.IO.Path.Combine(MainWindow.CACHE_PATH, id, s) + ".png"));
                if (lastExistingScreenshot != null)
                {
                    string screenshot = System.IO.Path.Combine(MainWindow.CACHE_PATH, id, lastExistingScreenshot) + ".png";
                    return ImageHelper.LoadImage(screenshot);
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
