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
        private EditText textHost;
        private EditText textLocation;
        private EditText textGroup;
        private Spinner spinnerDeviceType;

        private Device currentDevice = new Device();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            // ToDo: *** Furhter implemenation and parsing
            // Idea: Using a foreground service with a polling timer for SendAck

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);


            textHost = FindViewById<EditText>(Resource.Id.textHost);
            textLocation = FindViewById<EditText>(Resource.Id.textLocation);
            textGroup = FindViewById<EditText>(Resource.Id.textGroup);
            spinnerDeviceType = FindViewById<Spinner>(Resource.Id.spinnerDeviceType);


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

            // Read and assign memory info
            DeviceInfoHelper.ReadAndAssignMemoryInfo(currentDevice);

            currentDevice.Envoirnment.Vendor = Build.Brand;
            currentDevice.Envoirnment.MachineName = System.Environment.MachineName;
            currentDevice.Envoirnment.UserName = System.Environment.UserName;
            currentDevice.Envoirnment.Motherboard = Build.Board;
            currentDevice.Name = DeviceInfoHelper.GetDeviceName(ContentResolver); // System.Environment.MachineName;
            currentDevice.IP = DeviceInfoHelper.GetIpAddress(this);
        }

        private void BtnShowInfos_Click(object sender, System.EventArgs e)
        {
            Toast.MakeText(this, currentDevice.ToString(), ToastLength.Long).Show();
        }

        private void Renderer_OnInfosRecieved(string renderer, string vendor)
        {
            currentDevice.Envoirnment.Graphics = $"{vendor} {renderer}";

           
            btnShowInfos.Post( async () => { 
                
                //Home.Communication.API api = new Home.Communication.API(LocalConsts.)
            
            
            });
        }    
    }

    public class Renderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        public delegate void onInfosRecieved(string renderer, string vendor);
        public event onInfosRecieved OnInfosRecieved;

        public void OnDrawFrame(IGL10 gl)
        {
           
        }

        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            
        }

        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            string renderer = GLES20.GlGetString(GLES20.GlRenderer);
            string vendor = GLES20.GlGetString(GLES20.GlVendor);

            OnInfosRecieved?.Invoke(renderer, vendor);
        }
    }
}