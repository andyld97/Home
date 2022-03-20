using System;
using System.Windows.Media.Imaging;

namespace Home.Helper
{
    public static class ImageHelper
    {
        public static BitmapImage LoadImage(string bitmapSourceUri)
        {
            // ToDo: *** Cache resource images (prevint loading in multiple times)
            // ToDo: *** Add try catch and return default image?

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bi.UriSource = new Uri(bitmapSourceUri);
            bi.EndInit();

            return bi;
        }
    }
}
