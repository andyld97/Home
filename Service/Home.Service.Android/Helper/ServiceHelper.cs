using Android.Content;
using Android.OS;
using A = Android;
namespace Home.Service.Android.Helper
{
    internal class ServiceHelper
    {
        public static void StartAckService(Context context)
        {
            var intent = new A.Content.Intent(context, typeof(AckService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);
        }
    }
}