using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Widget;
using Android.Opengl;
using Home.Service.Android.Helper;
using Home.Model;
using A = Android;

namespace Home.Service.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Button btnShowInfos;
        private Button buttonRegisterDevice;

        private EditText textHost;
        private EditText textLocation;
        private EditText textGroup;
        private Spinner spinnerDeviceType;

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

            textHost = FindViewById<EditText>(Resource.Id.textHost);
            textLocation = FindViewById<EditText>(Resource.Id.textLocation);
            textGroup = FindViewById<EditText>(Resource.Id.textGroup);
            spinnerDeviceType = FindViewById<Spinner>(Resource.Id.spinnerDeviceType);
            
            buttonRegisterDevice = FindViewById<Button>(Resource.Id.buttonRegisterDevice);
            buttonRegisterDevice.Click += ButtonRegisterDevice_Click;

            btnShowInfos = FindViewById<Button>(Resource.Id.buttonShowInfos);
            btnShowInfos.Click += BtnShowInfos_Click;

            if (isDeviceRegistered)
            {
                SetGuiState(false);

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

            currentDevice.RefreshDevice(ContentResolver, this);

            if (isDeviceRegistered)
                StartAckService();
        }

        private void SetGuiState(bool value)
        {
            textHost.Enabled =
            textLocation.Enabled =
            textGroup.Enabled =
            spinnerDeviceType.Enabled =
            spinnerDeviceType.Enabled =
            buttonRegisterDevice.Enabled = value;
        }

        private void StartAckService()
        {
            var intent = new A.Content.Intent(this, typeof(AckService));
            // intent.PutExtra("host", currentSettings.Host);
            // intent.PutExtra("device_xml", Serialization.Serialization.SaveToString<Device>(currentDevice, System.Text.Encoding.Default));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                StartForegroundService(intent);
            else
                StartService(intent);
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

                SetGuiState(false);
                StartAckService();
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
    }
}