﻿using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Widget;
using Android.Opengl;
using Home.Service.Android.Helper;
using Home.Model;
using A = Android;
using System;

namespace Home.Service.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Button btnShowInfos;
        private Button buttonRegisterDevice;
        private Button buttonToggleService;
        private System.Timers.Timer serviceCheckingTimer;

        private EditText textHost;
        private EditText textLocation;
        private EditText textGroup;
        private Spinner spinnerDeviceType;
        private LinearLayout layoutRegisterDevice;

        private ImageView ledIsServiceRunning;
        private ImageView ledIsDeviceRegistered;

        private TextView textRegister;
        private TextView textService;

        private Device currentDevice;
        private Model.Settings currentSettings;

        private string xmlDevicePath = string.Empty;
        private string xmlSettingsPath = string.Empty;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            string baseDir = GetExternalFilesDir("device").AbsolutePath;
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
            bool isDeviceRegistered = false;

            if (System.IO.File.Exists(xmlSettingsPath))
            {
                try
                {
                    currentSettings = Serialization.Serialization.Read<Model.Settings>(xmlSettingsPath);
                    if (currentSettings == null)
                        currentSettings = new Model.Settings();
                }
                catch
                {

                }
            }
            else
                currentSettings = new Model.Settings();

            if (System.IO.File.Exists(xmlDevicePath))
            {
                try
                {
                    currentDevice = Serialization.Serialization.Read<Device>(xmlDevicePath);

                    if (currentDevice != null)
                        isDeviceRegistered = currentSettings.IsDeviceRegistered;
                }
                catch
                {
                    // Empty
                }

                if (currentDevice == null)
                    currentDevice = new Device();
            }
            else
                currentDevice = new Device();


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            layoutRegisterDevice = FindViewById<LinearLayout>(Resource.Id.layoutRegisterDevice);
            textHost = FindViewById<EditText>(Resource.Id.textHost);
            textLocation = FindViewById<EditText>(Resource.Id.textLocation);
            textGroup = FindViewById<EditText>(Resource.Id.textGroup);
            spinnerDeviceType = FindViewById<Spinner>(Resource.Id.spinnerDeviceType);            

            // LEDs
            ledIsServiceRunning = FindViewById<ImageView>(Resource.Id.ledIsServiceRunning);
            ledIsDeviceRegistered = FindViewById<ImageView>(Resource.Id.ledIsDeviceRegistered);

            // Buttons
            buttonRegisterDevice = FindViewById<Button>(Resource.Id.buttonRegisterDevice);
            btnShowInfos = FindViewById<Button>(Resource.Id.buttonShowInfos);
            buttonToggleService = FindViewById<Button>(Resource.Id.buttonToggleService);

            textRegister = FindViewById<TextView>(Resource.Id.textRegister);
            textService = FindViewById<TextView>(Resource.Id.textService);

            // Assign event handler
            buttonRegisterDevice.Click += ButtonRegisterDevice_Click;
            btnShowInfos.Click += BtnShowInfos_Click;
            buttonToggleService.Click += ButtonToggleService_Click;

            if (isDeviceRegistered)
            {
                textHost.Text = currentSettings.Host;
                textLocation.Text = currentDevice.Location;
                textGroup.Text = currentDevice.DeviceGroup;
                spinnerDeviceType.SetSelection((int)(currentDevice.Type - 5));
            }

            // Only determine graphics when it's not set, because GLSurfaceView/a valid Open GL Context is required
            if (string.IsNullOrEmpty(currentDevice.Envoirnment.Graphics))
            {
                GLSurfaceView glSurfaceView = FindViewById<GLSurfaceView>(Resource.Id.surface);
                Renderer renderer = new Renderer();
                renderer.OnInfosRecieved += delegate (string vendor, string renderer) { currentDevice.Envoirnment.Graphics = $"{vendor} {renderer}"; };
                glSurfaceView.SetRenderer(renderer);
            }
            else
            {
                GLSurfaceView glSurfaceView = FindViewById<GLSurfaceView>(Resource.Id.surface);
                glSurfaceView.Visibility = A.Views.ViewStates.Gone;
            }

            currentDevice.RefreshDevice(ContentResolver, this);

            if (isDeviceRegistered)
                ServiceHelper.StartAckService(this);

            RefreshServiceStatus();

            // Initalize serviceCheckingTimer
            serviceCheckingTimer = new System.Timers.Timer() { Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };
            serviceCheckingTimer.Elapsed += ServiceCheckingTimer_Elapsed;
            serviceCheckingTimer.Start();
        }

        private async void ButtonRegisterDevice_Click(object sender, System.EventArgs e)
        {
            string host = textHost.Text;

            // Assign further properties
            string location = textLocation.Text;
            string group = textGroup.Text;

            currentDevice.DeviceGroup = group;
            currentDevice.Location = location;
            var type = (Device.DeviceType)(spinnerDeviceType.SelectedItemId + 5);
            currentDevice.Type = type;

            // 0 => Smartphone : 5
            // 1 => SmartTV    : 6
            // 2 => SetTopBox  : 7
            Home.Communication.API api = new Home.Communication.API(host);

            var registerResult = await api.RegisterDeviceAsync(currentDevice);
            if (registerResult)
            {
                currentSettings.Host = host;
                currentSettings.IsDeviceRegistered = true;

                try
                {
                    // Save settings
                    Serialization.Serialization.Save(xmlSettingsPath, currentSettings);
                }
                catch
                { }

                try
                {
                    // Save device infos
                    Serialization.Serialization.Save(xmlDevicePath, currentDevice);
                }
                catch
                { }
            
                ServiceHelper.StartAckService(this);
                RefreshServiceStatus();
                Toast.MakeText(this, $"Das Gerät wurde erfolgreich registriert!", ToastLength.Short).Show();
            }
            else
                Toast.MakeText(this, $"Fehler beim Registrieren des Gerätes!", ToastLength.Short).Show();
        }

        private void BtnShowInfos_Click(object sender, System.EventArgs e)
        {
            currentDevice.RefreshDevice(ContentResolver, this);
            Toast.MakeText(this, currentDevice.ToString(), ToastLength.Long).Show();
        }

        #region Service Status
        private void SetGuiState(bool value)
        {
            /*textHost.Enabled =
            textLocation.Enabled =
            textGroup.Enabled =
            spinnerDeviceType.Enabled =
            spinnerDeviceType.Enabled =
            buttonRegisterDevice.Enabled = value; */
            layoutRegisterDevice.Visibility = (value ? A.Views.ViewStates.Visible : A.Views.ViewStates.Gone);
            buttonToggleService.Enabled = currentSettings.IsDeviceRegistered;
        }

        /// <summary>
        ///  ToDo: This methods need to be called in a timer periodically
        /// </summary>
        private void RefreshServiceStatus()
        {
            bool isServiceRunning = ServiceHelper.IsMyServiceRunning(this, typeof(AckService));
            bool isDeviceRegistered = currentSettings.IsDeviceRegistered;
            
            SetGuiState(!isDeviceRegistered);

            // Assign leds
            ledIsDeviceRegistered.SetImageResource(isDeviceRegistered ? Resource.Drawable.led_on : Resource.Drawable.led_off  );
            ledIsServiceRunning.SetImageResource(isServiceRunning ? Resource.Drawable.led_on : Resource.Drawable.led_off);

            // Assing texts
            textRegister.Text = (isDeviceRegistered ? $"Das Gerät {currentDevice.Name} ist registiert!" : $"Das Gerät {currentDevice.Name} nicht ist registiert!");
            textService.Text = (isServiceRunning ? "Home.Service.Android ist aktiv!" : "Home.Service.Android nicht ist aktiv!");

            buttonToggleService.Text = (isServiceRunning ? "Service beenden" : "Service starten");
        }

        private void ButtonToggleService_Click(object sender, System.EventArgs e)
        {
            bool isServiceRunning = ServiceHelper.IsMyServiceRunning(this, typeof(AckService));

            if (!isServiceRunning)
                ServiceHelper.StartAckService(this);
            else 
                ServiceHelper.StopAckService(this); 

            RefreshServiceStatus();
        }

        private void ServiceCheckingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RefreshServiceStatus();
        }
        #endregion
    }
}