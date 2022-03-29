using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Home.Helper
{
    public static class ImageHelper
    {
        public static BitmapImage LoadImage(string bitmapSourceUri, bool isScreenshot)
        {
            // ToDo: *** Cache resource images (prevint loading in multiple times)
            if (isScreenshot && !System.IO.File.Exists(bitmapSourceUri))
            {
                // Replace with default image uri
                bitmapSourceUri = "pack://application:,,,/Home;Component/resources/images/screenshot_default.png";
            }

            try
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bi.UriSource = new Uri(bitmapSourceUri);
                bi.EndInit();

                return bi;
            }
            catch
            {
                return null;
            }
        }

        public static BitmapImage LoadImage(System.IO.Stream stream)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bi.StreamSource = stream;
            bi.EndInit();

            return bi;
        }

        public static FormatConvertedBitmap GrayscaleBitmap(BitmapImage bi)
        {
            FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap();
            grayBitmap.BeginInit();
            grayBitmap.Source = bi;
            grayBitmap.DestinationFormat = PixelFormats.Gray8;
            grayBitmap.EndInit();

            return grayBitmap;
        }
    }
}