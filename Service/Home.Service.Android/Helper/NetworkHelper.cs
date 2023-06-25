using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using A = Android;

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

        public static string GetWLANSSID(Context context)
        {
            // Ensure device is connected to WLAN
            if (!IsConnectedToWLAN(context))
                return null;

            // Ensure that ACCESS_FINE_LOCATION is set
            if (context.CheckSelfPermission(A.Manifest.Permission.AccessFineLocation) == A.Content.PM.Permission.Denied)
                return null;

            try
            {
                ConnectivityManager connManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
                NetworkInfo networkInfo = connManager.GetNetworkInfo(ConnectivityType.Wifi);
                WifiManager wifiManager = (WifiManager)context.ApplicationContext.GetSystemService(Context.WifiService);
                WifiInfo wifiInfo = wifiManager.ConnectionInfo;

                if (!string.IsNullOrEmpty(wifiInfo.SSID))
                {
                    if (wifiInfo.SSID == "<unknown ssid>")
                        return null;

                    return wifiInfo.SSID.Replace("\"", string.Empty);
                }
            }
            catch
            { }

            return null;
        }

        private static NetworkInfo GetActiveNetworkInfo(Context context)
        {
            ConnectivityManager connManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);            
            return connManager.ActiveNetworkInfo;
        }
    }
}