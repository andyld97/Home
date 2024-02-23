using Android;
using Android.Content;
using Home.Service.Android.Helper;

namespace Home.Service.Android
{
    // https://stackoverflow.com/questions/29731435/broadcast-receiver-not-receiving-on-boot-complete-in-android-kitkat
    // https://stackoverflow.com/questions/20441308/boot-completed-not-working-android
    // App must be started once to enable BroadcastReciever
    [BroadcastReceiver(Enabled = true, Exported = true, Permission = Manifest.Permission.ReceiveBootCompleted)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted, "android.intent.action.QUICKBOOT_POWERON"}, Priority = (int)IntentFilterPriority.HighPriority, Categories = new[] { Intent.CategoryDefault })]
    public class OnBootBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            ServiceHelper.StartAckService(context);
        }
    }
}