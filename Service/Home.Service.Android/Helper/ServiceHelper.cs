using Android.App;
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

        public static void StopAckService(Context context)
        {
            var intent = new A.Content.Intent(context, typeof(AckService));
            context.StopService(intent);
        }

        public static bool IsMyServiceRunning(Context context, System.Type cls)
        {
            ActivityManager manager = (ActivityManager)context.GetSystemService(Context.ActivityService);

            foreach (var service in manager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.Equals(Java.Lang.Class.FromType(cls).CanonicalName))
                    return true;
            }

            return false;
        }
    }
}