using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Home.Model;
using Home.Service.Android.Helper;
using System;
using System.IO;
using static Android.Net.ConnectivityManager;
using A = Android;

namespace Home.Service.Android
{
    /// <summary>
    /// Taken from https://androidwave.com/foreground-service-android-example/
    /// </summary>
    [Service(DirectBootAware = true, Enabled = true, Exported = true, Name = "Home.Service.Android.AckService", ForegroundServiceType = A.Content.PM.ForegroundService.TypeSpecialUse)]
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

            // Supporting Android 12 (Immutable Pending Intent)
            // https://developer.android.com/guide/components/intents-filters#DeclareMutabilityPendingIntent
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, PendingIntentFlags.Immutable);

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
            string ssid = NetworkHelper.GetWLANSSID(this);
            NotificationCompat.Builder notificationBuilder = new NotificationCompat.Builder(this, CHANNEL_ID)
                    .SetContentTitle("Home.Service.Android")
                    .SetContentText(GetNotificationText(ssid))
                    .SetSmallIcon(Resource.Drawable.settings)
                    .SetContentIntent(pendingIntent);

            var notification = notificationBuilder.Build();

            // https://stackoverflow.com/a/77530440/6237448
            if (A.OS.Build.VERSION.SdkInt > BuildVersionCodes.Tiramisu)
                StartForeground(1, notification, A.Content.PM.ForegroundService.TypeSpecialUse);
            else
                StartForeground(1, notification);

            // Start timer
            serviceTimer = new System.Threading.Timer(async delegate (object state)
            {
                bool isConnected = true;
                string additionalMessage = string.Empty;
                string ssid = NetworkHelper.GetWLANSSID(this);

                if (!NetworkHelper.IsConnectedToWLAN(this))
                    isConnected = false;
                else
                {
                    if (!string.IsNullOrEmpty(settings.WlanSSID) && !string.IsNullOrEmpty(ssid) && settings.WlanSSID != ssid)
                        isConnected = false;
                }

                string message = GetNotificationText(ssid);
                if (!isConnected)
                {           
                    UpdateTextNotification(message, notificationBuilder);
                    return;
                }
                else
                    UpdateTextNotification(message, notificationBuilder);

                // Refresh device information (preparing ack ...)
                currentDevice.RefreshDevice(ApplicationContext.ContentResolver, ApplicationContext);
                
                // Send device ack to the api
                await api.SendAckAsync(currentDevice);
            
            }, null, 0, (int)TimeSpan.FromSeconds(30).TotalMilliseconds);

            return StartCommandResult.Sticky;
        }

        private string GetNotificationText(string ssid)
        {
            string additionalMessage = string.Empty;
            bool isConnected = true;

            if (!NetworkHelper.IsConnectedToWLAN(this))
            {
                isConnected = false;
                return GetString(Resource.String.strNotConnected);
            }
            else
            {
                if (!string.IsNullOrEmpty(settings.WlanSSID))
                {
                    if (!string.IsNullOrEmpty(ssid))
                    {
                        if (settings.WlanSSID != ssid)
                        {
                            // Other WLAN
                            isConnected = false;
                            additionalMessage = $"({ssid})";
                        }
                    }
                }
            }

            if (!isConnected)
            {
                string message = GetString(Resource.String.strNotConnected);
                if (!string.IsNullOrEmpty(additionalMessage))
                    message += $" {additionalMessage}";

                return message; 
            }
            else
               return string.Format(GetString(Resource.String.strConnectedTo), settings.Host);
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

            PendingIntent restartServicePendingIntent = PendingIntent.GetService(ApplicationContext, 1, restartServiceIntent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
            AlarmManager alarmService = (AlarmManager)ApplicationContext.GetSystemService(Context.AlarmService);
            alarmService.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + 1000, restartServicePendingIntent);

            base.OnTaskRemoved(rootIntent);
        }
    }
}