using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Home.Model;
using Home.Service.Android.Helper;
using System;
using A = Android;

namespace Home.Service.Android
{

    // ToDo: *** Start on Boot (using BroadcastReciever)

    /// <summary>
    /// Taken from https://androidwave.com/foreground-service-android-example/
    /// </summary>
    [Service(DirectBootAware = true, Enabled = true, Exported = true, Name = "Home.Service.Android.AckService")]
    public class AckService : A.App.Service
    {
        public static readonly string CHANNEL_ID = "ForegroundAckServiceChannel";
        private string lastMessage = string.Empty;
        private System.Threading.Timer serviceTimer;

        private Device currentDevice;
        private Model.Settings settings;

        private Home.Communication.API api;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel(ApplicationContext);
            Intent notificationIntent = new Intent(this, typeof(MainActivity));
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, 0);

            // Get settings and currentDevice
            string xmlDevicePath = string.Empty;
            string xmlSettingsPath = string.Empty;


            string baseDir = ApplicationContext.GetExternalFilesDir("device").AbsolutePath;
            if (!System.IO.Directory.Exists(baseDir))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(baseDir);
                }
                catch
                {

                }
            }

            // Check if the device is already registered
            xmlDevicePath = System.IO.Path.Combine(baseDir, "device.xml");
            xmlSettingsPath = System.IO.Path.Combine(baseDir, "settings.xml");

            // Load settings and currentDevice
            try
            {
                currentDevice = Serialization.Serialization.Read<Device>(xmlDevicePath);
                settings = Serialization.Serialization.Read<Model.Settings>(xmlSettingsPath);

                if (currentDevice == null || settings == null)
                    throw new ArgumentNullException("currentDevice or settings must be set!");

                if (!settings.IsDeviceRegistered)
                    throw new ArgumentException("Device is not registered, so the service cannot start");

                api = new Communication.API(settings.Host);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"Failed to start service: {ex}");
                StopSelf();
                return StartCommandResult.NotSticky; 
            }

            // Create notification
            string textConntected = $"Verbunden mit {settings.Host}";
            NotificationCompat.Builder notificationBuilder = new NotificationCompat.Builder(this, CHANNEL_ID)
                    .SetContentTitle("Home.Service.Android")
                    .SetContentText(textConntected)
                    .SetSmallIcon(Resource.Drawable.settings)
                    .SetContentIntent(pendingIntent);

            var notification = notificationBuilder.Build();
            StartForeground(1, notification);

            // Start timer
            serviceTimer = new System.Threading.Timer(async delegate (object state)
            {
                if (!NetworkHelper.IsConnectedToWLAN(this))
                {
                    UpdateTextNotification("Nicht verbunden", notificationBuilder);
                    return;
                }
                else
                    UpdateTextNotification(textConntected, notificationBuilder);

                // Refresh device information (preparing ack ...)
                currentDevice.RefreshDevice(ApplicationContext.ContentResolver, ApplicationContext);
                
                // Send device ack to the api
                await api.SendAckAsync(currentDevice);
            
            }, null, 0, (int)TimeSpan.FromSeconds(30).TotalMilliseconds);

            return StartCommandResult.Sticky;
        }

        private void UpdateTextNotification(string text, NotificationCompat.Builder builder)
        {
            // Prevent unnecessary updates
            if (lastMessage == text)
                return;

            NotificationManager manager = (NotificationManager)ApplicationContext.GetSystemService(Context.NotificationService);
            builder.SetContentText(text);
            manager.Notify(1, builder.Build());

            lastMessage = text;
        }

        private void CreateNotificationChannel(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel serviceChannel = new NotificationChannel(CHANNEL_ID, "Foreground Service Channel", NotificationImportance.Default);
                NotificationManager manager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                manager.CreateNotificationChannel(serviceChannel);
            }
        }

        /// <summary>
        /// Restart the service if it get's removed: https://stackoverflow.com/a/43310945/6237448
        /// </summary>
        /// <param name="rootIntent"></param>
        public override void OnTaskRemoved(Intent rootIntent)
        {
            Intent restartServiceIntent = new Intent(ApplicationContext, this.GetType());
            restartServiceIntent.SetPackage(ApplicationContext.PackageName);

            PendingIntent restartServicePendingIntent = PendingIntent.GetService(ApplicationContext, 1, restartServiceIntent, PendingIntentFlags.OneShot);
            AlarmManager alarmService = (AlarmManager)ApplicationContext.GetSystemService(Context.AlarmService);
            alarmService.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + 1000, restartServicePendingIntent);

            base.OnTaskRemoved(rootIntent);
        }
    }
}