using Android.Content;
using Android.Net;

namespace Home.Service.Android.Helper
{
    /// <summary>
    /// Taken from https://stackoverflow.com/a/54473152/6237448
    /// </summary>
    public static class NetworkHelper
    {
        public static bool IsConnectedToWLAN(Context context)
        {
            try
            {
                NetworkInfo net = GetActiveNetworkInfo(context);
                return net.IsConnected && net.Type == ConnectivityType.Wifi;
            }
            catch
            {

            }

            return false;
        }
        private static NetworkInfo GetActiveNetworkInfo(Context context)
        {
            ConnectivityManager connManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);            
            return connManager.ActiveNetworkInfo;
        }
    }
}