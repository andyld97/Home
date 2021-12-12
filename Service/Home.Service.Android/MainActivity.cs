using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Java.Lang;
using Android.Widget;
using Android.Hardware;
using Android.Text.Method;
using Java.Text;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using Home.Service.Android.Helper;
using Home.Model;

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

        private Device currentDevice = new Device();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ToDo: *** Further implemenation and parsing
            // Idea: Using a foreground service with a polling timer for SendAck

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

            GLSurfaceView glSurfaceView = FindViewById<GLSurfaceView>(Resource.Id.surface);
            Renderer renderer = new Renderer();
            renderer.OnInfosRecieved += Renderer_OnInfosRecieved;
            glSurfaceView.SetRenderer(renderer);

            currentDevice.ServiceClientVersion = "vAndroid 0.0.1";
            currentDevice.Type = Device.DeviceType.Smartphone;
            currentDevice.Envoirnment.OSName = $"Android {Build.VERSION.Release}";
            currentDevice.Envoirnment.OSVersion = $"{Build.VERSION.Codename} (Sec. Patch: {Build.VERSION.SecurityPatch}) ({System.Environment.OSVersion})";
            currentDevice.OS = Device.OSType.Android;
            currentDevice.Envoirnment.CPUCount = DeviceInfoHelper.GetNumberOfCores();
            currentDevice.Envoirnment.CPUName = DeviceInfoHelper.ReadCPUName();
            currentDevice.Envoirnment.Description = Build.Model;
            currentDevice.Envoirnment.DomainName = System.Environment.UserDomainName;
            currentDevice.Envoirnment.Is64BitOS = System.Environment.Is64BitOperatingSystem;
            currentDevice.Envoirnment.Product = Build.Product;
            currentDevice.Envoirnment.StartTimestamp = System.DateTime.Now;

            // Read and assign memory info
            DeviceInfoHelper.ReadAndAssignMemoryInfo(currentDevice);

            currentDevice.Envoirnment.Vendor = Build.Brand;
            currentDevice.Envoirnment.UserName = System.Environment.UserName;
            currentDevice.Envoirnment.Motherboard = Build.Board;
            currentDevice.Envoirnment.MachineName = 
            currentDevice.Name = DeviceInfoHelper.GetDeviceName(ContentResolver);
            currentDevice.IP = DeviceInfoHelper.GetIpAddress(this);
        }


        private async void ButtonRegisterDevice_Click(object sender, System.EventArgs e)
        {
            Home.Communication.API api = new Home.Communication.API(textHost.Text);
            var client = new Home.Data.Client() { IsRealClient = true, Name = currentDevice.Name, ID = currentDevice.ID };
            var loginResult = await api.LoginAsync(client);


            var ackResult = await api.RegisterDeviceAsync(currentDevice);



            var test = await api.LogoffAsync(client);

            int debug = 0;
        }

        private void BtnShowInfos_Click(object sender, System.EventArgs e)
        {
            Toast.MakeText(this, currentDevice.ToString(), ToastLength.Long).Show();
        }

        private void Renderer_OnInfosRecieved(string renderer, string vendor)
        {
            currentDevice.Envoirnment.Graphics = $"{vendor} {renderer}";
        }
    }
}