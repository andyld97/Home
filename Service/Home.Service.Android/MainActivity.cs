using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Widget;
using Android.Opengl;
using Home.Service.Android.Helper;
using Home.Model;
using A = Android;
using System;
using System.Collections.Generic;
using Android.Content;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Android.Runtime;
using System.Runtime.InteropServices;
using Android.Provider;
using System.Threading.Tasks;
using AndroidX.Core.App;
using Android.Views;
using Android.Text;

namespace Home.Service.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Button buttonRegisterDevice;
        private Button buttonToggleService;
        private ImageButton btnCurrent;
        private Button btnEditSettings;
        private Button buttonCheckPermissions;
        private System.Timers.Timer serviceCheckingTimer;

        private TextView textDeviceID;
        private EditText textHost;
        private EditText textLocation;
        private EditText textGroup;
        private EditText textWLANSSID;
        private Spinner spinnerDeviceType;
        private LinearLayout layoutRegisterDevice;

        private ImageView ledIsServiceRunning;
        private ImageView ledIsDeviceRegistered;
        private ImageView ledPermissionGranted;

        private TextView textRegister;
        private TextView textService;
        private TextView textPermissions;
        private TextView textCaption;

        private Device currentDevice;
        private Model.Settings currentSettings;

        private string xmlDevicePath = string.Empty;
        private string xmlSettingsPath = string.Empty;

        private bool isInEditMode = false;


        private Dictionary<int, Device.DeviceType> spinnerAssoc = new Dictionary<int, Device.DeviceType>()
        {
            { 0, Device.DeviceType.Smartphone },
            { 1, Device.DeviceType.SmartTV },
            { 2, Device.DeviceType.SetTopBox },
            { 3, Device.DeviceType.Tablet },
            { 4, Device.DeviceType.AndroidTVStick },
        };

        private List<string> permissions = new List<string>()
        {
            A.Manifest.Permission.WriteExternalStorage,
            A.Manifest.Permission.ReadExternalStorage,
            A.Manifest.Permission.AccessFineLocation,
            A.Manifest.Permission.AccessBackgroundLocation,
        };

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
            textDeviceID = FindViewById<TextView>(Resource.Id.textDeviceID);
            textHost = FindViewById<EditText>(Resource.Id.textHost);
            textLocation = FindViewById<EditText>(Resource.Id.textLocation);
            textGroup = FindViewById<EditText>(Resource.Id.textGroup);
            textWLANSSID = FindViewById<EditText>(Resource.Id.textWLANSSID);
            textPermissions = FindViewById<TextView>(Resource.Id.textPermissions);
            textCaption = FindViewById<TextView>(Resource.Id.textCaption);
            
            spinnerDeviceType = FindViewById<Spinner>(Resource.Id.spinnerDeviceType);
            btnEditSettings = FindViewById<Button>(Resource.Id.buttonEditSettings);
            btnCurrent = FindViewById<ImageButton>(Resource.Id.btnCurrent);
            btnCurrent.Click += BtnCurrent_Click;

            // LEDs
            ledIsServiceRunning = FindViewById<ImageView>(Resource.Id.ledIsServiceRunning);
            ledIsDeviceRegistered = FindViewById<ImageView>(Resource.Id.ledIsDeviceRegistered);
            ledPermissionGranted = FindViewById<ImageView>(Resource.Id.ledPermissionGranted);

            // Buttons
            buttonRegisterDevice = FindViewById<Button>(Resource.Id.buttonRegisterDevice);
            buttonToggleService = FindViewById<Button>(Resource.Id.buttonToggleService);
            buttonCheckPermissions = FindViewById<Button>(Resource.Id.buttonCheckPermissions);

            textRegister = FindViewById<TextView>(Resource.Id.textRegister);
            textService = FindViewById<TextView>(Resource.Id.textService);

            // Assign event handler
            buttonRegisterDevice.Click += ButtonRegisterDevice_Click;
            buttonToggleService.Click += ButtonToggleService_Click;
            btnEditSettings.Click += BtnEditSettings_Click;
            buttonCheckPermissions.Click += ButtonCheckPermissions_Click;

            if (isDeviceRegistered)
            {
                textHost.Text = currentSettings.Host;
                textLocation.Text = currentDevice.Location;
                textGroup.Text = currentDevice.DeviceGroup;
                textWLANSSID.Text = currentSettings.WlanSSID;
                textDeviceID.TextFormatted = Html.FromHtml($"ID: <b>{currentDevice.ID}</b>", FromHtmlOptions.ModeCompact);
                btnEditSettings.Visibility = A.Views.ViewStates.Visible;

                foreach (var item in spinnerAssoc)
                    if (item.Value == currentDevice.Type)
                        spinnerDeviceType.SetSelection(item.Key);
            }
            else
                btnEditSettings.Visibility = A.Views.ViewStates.Gone;

            textGroup.NextFocusDownId = Resource.Id.spinnerDeviceType;

#if !NOGL
            // Only determine graphics when it's not set, because GLSurfaceView/a valid Open GL Context is required
            if (currentDevice.Environment.GraphicCards.Count == 0)
            {
                GLSurfaceView glSurfaceView = FindViewById<GLSurfaceView>(Resource.Id.surface);
                Renderer renderer = new Renderer();
                renderer.OnInfosReceived += delegate (string vendor, string renderer) { currentDevice.Environment.GraphicCards = new System.Collections.ObjectModel.ObservableCollection<string> { $"{vendor} {renderer}" }; };
                glSurfaceView.SetRenderer(renderer);
            }
            else
            {
                GLSurfaceView glSurfaceView = FindViewById<GLSurfaceView>(Resource.Id.surface);
                glSurfaceView.Visibility = A.Views.ViewStates.Gone;
            }
#else
            if (currentDevice.Environment.GraphicCards.Count == 0)
                currentDevice.Environment.GraphicCards.Add("Unknown");
#endif

            currentDevice.RefreshDevice(ContentResolver, this);

            if (CheckPermissions())
            {
                if (isDeviceRegistered)
                    ServiceHelper.StartAckService(this);

                RefreshServiceStatus();
            }
            else
                RefreshServiceStatus();

            // Initialize serviceCheckingTimer
            serviceCheckingTimer = new System.Timers.Timer() { Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };
            serviceCheckingTimer.Elapsed += ServiceCheckingTimer_Elapsed;
            serviceCheckingTimer.Start();   
        }

        private void ButtonCheckPermissions_Click(object sender, EventArgs e)
        {
            CheckPermissions();
            RefreshServiceStatus();
        }

        protected override void OnResume()
        {
            base.OnResume();
            RefreshServiceStatus();
        }

        #region Menu

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater inflater = MenuInflater;
            inflater.Inflate(Resource.Menu.menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menuButtonShowSpecs:
                    {
                        currentDevice.RefreshDevice(ContentResolver, this);
                        Dialog diag = null;

                        // Show dialog instead off a toast message
                        string info = currentDevice.ToString();

                        var wlanSSID = NetworkHelper.GetWLANSSID(this);

                        if (!string.IsNullOrEmpty(wlanSSID))
                            info += $"{System.Environment.NewLine}WLAN-SSID: {wlanSSID}";

                        info += System.Environment.NewLine;
                        info += $"Client-Version: vAndroid{typeof(MainActivity).Assembly.GetName().Version.ToString(3)}";

                        AlertDialog.Builder alertDiag = new AlertDialog.Builder(this);
                        alertDiag.SetTitle(GetString(Resource.String.strDeviceSpecifications));
                        alertDiag.SetMessage(info);
                        alertDiag.SetPositiveButton("OK", (senderAlert, args) => {
                            diag.Dismiss();
                        });
                        diag = alertDiag.Create();
                        diag.Show();
                        return true;
                    }
                case Resource.Id.menuAbout:
                    {
                        Dialog diag = null;

                        AlertDialog.Builder alertDiag = new AlertDialog.Builder(this);
                        alertDiag.SetTitle(GetString(Resource.String.strAbout));
                        alertDiag.SetMessage(string.Format(GetString(Resource.String.strAboutContent), typeof(MainActivity).Assembly.GetName().Version.ToString(3)));
                        alertDiag.SetPositiveButton("OK", (senderAlert, args) => {
                            diag.Dismiss();
                        });
                        diag = alertDiag.Create();
                        diag.Show();
                        return true;
                    }
                default: return base.OnOptionsItemSelected(item);
            }
        }

        #endregion

        private void BtnEditSettings_Click(object sender, EventArgs e)
        {
            // https://stackoverflow.com/a/14945159/6237448
            layoutRegisterDevice.Post(async () =>
            {
                if (isInEditMode)
                {
                    await ApplySettings(false);

                    layoutRegisterDevice.Visibility = A.Views.ViewStates.Gone;
                    layoutRegisterDevice.RequestLayout();
                    btnEditSettings.Text = GetString(Resource.String.strEditSettings);
                    isInEditMode = false;

                    RefreshServiceStatus();
                    serviceCheckingTimer.Start();
                }
                else
                {
                    bool isServiceRunning = ServiceHelper.IsServiceRunning(this, typeof(AckService));

                    if (isServiceRunning)
                        ServiceHelper.StopAckService(this);

                    serviceCheckingTimer.Stop();

                    isInEditMode = true;
                    RefreshServiceStatus();
                    layoutRegisterDevice.Visibility = A.Views.ViewStates.Visible;
                    layoutRegisterDevice.RequestLayout();
                    btnEditSettings.Text = GetString(Resource.String.strSave);
                    btnCurrent.Enabled = true;
                    buttonRegisterDevice.Enabled = false;
                    buttonToggleService.Enabled = false;
                }
            });
        }

        private void BtnCurrent_Click(object sender, EventArgs e)
        {
            CheckPermissions();

            var ssid = NetworkHelper.GetWLANSSID(this);

            if (string.IsNullOrEmpty(ssid))
                Toast.MakeText(this, GetString(Resource.String.strFailedToDetermineWLANSSID), ToastLength.Short).Show();
            else
                textWLANSSID.Text = ssid;
        }

        private bool CheckPermissions(bool onlyCheck = false)
        {
            // Storage for file access; Fine Location for WiFi SSID

            // https://stackoverflow.com/a/69672683/6237448:
            // TL;DR: BACKGROUND_LOCATION permission must be asked for separately since API 30
            // User has to click multiple times on the button and grant all permissions in order to run the service.
            // Android Permission System really sucks :(

            bool requestPermissions = false;
            bool isStoragePermission = false;
            string permission_ = string.Empty;

            int requestCode = 1000;
            int index = 1;
            foreach (var permission in permissions) 
            {
                if (CheckSelfPermission(permission) == A.Content.PM.Permission.Denied)
                {
                    permission_ = permission;   
                    if (permission == A.Manifest.Permission.WriteExternalStorage || permission == A.Manifest.Permission.ReadExternalStorage)
                        isStoragePermission = true;

                    requestPermissions = true;
                    requestCode += index;
                    break;
                }
                index++;
            }

            if (requestPermissions && !onlyCheck)
            {
                if (A.OS.Build.VERSION.SdkInt < BuildVersionCodes.R)
                    ActivityCompat.RequestPermissions(this, permissions.ToArray(), requestCode);
                else
                {
                    if (isStoragePermission)
                        ActivityCompat.RequestPermissions(this, new string[] { A.Manifest.Permission.ReadExternalStorage, A.Manifest.Permission.WriteExternalStorage }, requestCode);
                    else
                        ActivityCompat.RequestPermissions(this, new string[] { permission_ }, requestCode);
                }
            }

            return (!requestPermissions);
        }        

        private async Task ApplySettings(bool register)
        {
            bool success = false;
            string host = textHost.Text;

            // Assign further properties
            string location = textLocation.Text;
            string group = textGroup.Text;
            string wlanSSID = textWLANSSID.Text;

            currentDevice.DeviceGroup = group;
            currentDevice.Location = location;
            currentDevice.Type = spinnerAssoc[(int)spinnerDeviceType.SelectedItemId];
            currentSettings.WlanSSID = wlanSSID;
            Home.Communication.API api = new Home.Communication.API(host);

            (bool, string) registerResult;
            if (register)
                registerResult = await api.RegisterDeviceAsync(currentDevice);
            else
                registerResult = (true, string.Empty);

            if (registerResult.Item1)
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

                success = true;                
            }
            else
                Toast.MakeText(this, $"{GetString(Resource.String.strDeviceRegisterFail)} ({registerResult.Item2})", ToastLength.Short).Show();

            if (success)
            {
                ServiceHelper.StartAckService(this);
                RefreshServiceStatus();

                if (register)
                    Toast.MakeText(this, GetString(Resource.String.strDeviceRegisterSuccess), ToastLength.Short).Show();
                else 
                    Toast.MakeText(this, GetString(Resource.String.strSettingsApplied), ToastLength.Short).Show(); 
            }
        }

        private async void ButtonRegisterDevice_Click(object sender, System.EventArgs e)
        {
            await ApplySettings(true);
        }

        #region Service Status

        private void SetGuiState(bool value)
        {
            if (isInEditMode)
                return;

            layoutRegisterDevice.Visibility = (value ? A.Views.ViewStates.Visible : A.Views.ViewStates.Gone);
            buttonToggleService.Enabled = currentSettings.IsDeviceRegistered;
        }

        private void RefreshServiceStatus()
        {
            bool isServiceRunning = ServiceHelper.IsServiceRunning(this, typeof(AckService));
            bool isDeviceRegistered = currentSettings.IsDeviceRegistered;
            bool arePermissionGranted = CheckPermissions(true);

            if (isDeviceRegistered)
                btnEditSettings.Visibility = A.Views.ViewStates.Visible;
            else
                btnEditSettings.Visibility = A.Views.ViewStates.Gone;

            if (CheckPermissions(true))
                buttonCheckPermissions.Visibility = ViewStates.Gone;
            else
                buttonCheckPermissions.Visibility = ViewStates.Visible;

            SetGuiState(!isDeviceRegistered);

            // Assign LEDs
            ledIsDeviceRegistered.SetImageResource(isDeviceRegistered ? Resource.Drawable.led_on : Resource.Drawable.led_off);
            ledIsServiceRunning.SetImageResource(isServiceRunning ? Resource.Drawable.led_on : Resource.Drawable.led_off);
            ledPermissionGranted.SetImageResource(arePermissionGranted ? Resource.Drawable.led_on : Resource.Drawable.led_off);

            // Assign texts
            textRegister.Text = (isDeviceRegistered ? string.Format(GetString(Resource.String.strDeviceRegisteredText), currentDevice.Name) : string.Format(GetString(Resource.String.strDeviceNotRegisteredText), currentDevice.Name));
            textPermissions.Text = (arePermissionGranted ? GetString(Resource.String.strPermissionsGranted) : GetString(Resource.String.strPermissionsDenied));
            textCaption.Text = (isDeviceRegistered ? GetString(Resource.String.strEditSettings) : GetString(Resource.String.strRegisterDevice));
            textService.Text = string.Format((isServiceRunning ? GetString(Resource.String.strServiceActiveText) : GetString(Resource.String.strServiceInActiveText)), $"v{typeof(MainActivity).Assembly.GetName().Version.ToString(3)}");
            buttonToggleService.Text = (isServiceRunning ? GetString(Resource.String.strStopService) : GetString(Resource.String.strStartService));
        }

        private void ButtonToggleService_Click(object sender, System.EventArgs e)
        {
            bool isServiceRunning = ServiceHelper.IsServiceRunning(this, typeof(AckService));

            if (!isServiceRunning && !CheckPermissions())
            {
                Toast.MakeText(this, GetString(Resource.String.strPermissionsExplanation), ToastLength.Long).Show();
                return;
            }

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