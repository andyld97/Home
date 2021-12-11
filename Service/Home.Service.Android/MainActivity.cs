using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using System;
using Java.Lang;
using Process = Java.Lang.Process;
using Android.Widget;
using Android.Hardware;
using System.Collections.Generic;
using Android.Text.Method;
using Java.Text;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;

namespace Home.Service.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private TextView tv;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            // ToDo: *** Furhter implemenation and parsing
            // Idea: Using a foreground service with a polling timer for SendAck

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            tv = FindViewById<TextView>(Resource.Id.textTest);
            GLSurfaceView glSurfaceView = FindViewById<GLSurfaceView>(Resource.Id.test);
            Renderer renderer = new Renderer();
            renderer.OnInfosRecieved += Renderer_OnInfosRecieved;
            glSurfaceView.SetRenderer(renderer);

            string arch = Java.Lang.JavaSystem.GetProperty("os.arch");

            tv.Text = "***** DEVICE Information *****" + "\n";
            tv.Append("Model: " + Build.Model + "\n");
            tv.Append("Board: " + Build.Board + "\n");
            tv.Append("Brand: " + Build.Brand + "\n");
            tv.Append("Manufacturer: " + Build.Manufacturer + "\n");
            tv.Append("Device: " + Build.Device + "\n");
            tv.Append("Product: " + Build.Product + "\n");
            tv.Append("TAGS: " + Build.Tags + "\n");
            tv.Append("Serial: " + Build.Serial + "\n");

            tv.Append("\n" + "***** SOC *****" + "\n");
            tv.Append("Hardware: " + Build.Hardware + "\n");
            tv.Append("Number of cores: " + GetNumberOfCores() + "\n");
            tv.Append("Architecture: " + arch + "\n");

            tv.Append("\n" + "***** CPU Info *****" + "\n");
            tv.Append(ReadCPUinfo() + "\n");

            tv.Append("\n" + "***** Memory Info *****" + "\n");
            tv.Append(ReadMemoryInfo() + "\n");

            tv.Append("\n" + "***** OS Information *****" + "\n");
            tv.Append("Build release: " + Build.VERSION.Release + "\n");
            tv.Append("Incremental release: " + Build.VERSION.Incremental + "\n");
            tv.Append("Base OS: " + Build.VERSION.BaseOs + "\n");
            tv.Append("CODE Name: " + Build.VERSION.Codename + "\n");
            tv.Append("Security patch: " + Build.VERSION.SecurityPatch + "\n");
            tv.Append("Preview SDK: " + Build.VERSION.PreviewSdkInt + "\n");
            tv.Append("SDK/API version: " + Build.VERSION.SdkInt + "\n");
            tv.Append("Display build: " + Build.Display + "\n");
            tv.Append("Finger print: " + Build.Fingerprint + "\n");
            tv.Append("Build ID: " + Build.Id + "\n");

            SimpleDateFormat sdf = new SimpleDateFormat("MMMM d, yyyy 'at' h:mm a");
            string date = sdf.Format(Build.Time);

            tv.Append("Build Time: " + date + "\n");
            tv.Append("Build Type: " + Build.Type + "\n");
            tv.Append("Build User: " + Build.User + "\n");
            tv.Append("Bootloader: " + Build.Bootloader + "\n");
            tv.Append("Kernel version: " + Java.Lang.JavaSystem.GetProperty("os.version") + "\n");

            tv.Append("\n" + "***** RADIO version *****" + "\n");
            tv.Append(Build.RadioVersion + "\n");

            /* List of available sensors */
            SensorManager sm = (SensorManager)GetSystemService("sensor");
            var list = sm.GetSensorList(SensorType.All);

            tv.Append("\n" + "***** SENSORS Information *****" + "\n");
            foreach (Sensor s in list)
            {
                tv.Append(s.Name + "\n");
            }

            tv.SetHorizontallyScrolling(true);
            tv.MovementMethod = new ScrollingMovementMethod();
        }

        private void Renderer_OnInfosRecieved(string renderer, string vendor)
        {
            tv.Append(renderer + "\n");
            tv.Append(vendor + "\n");
        }

        public string ReadMemoryInfo()
        {
            Java.Lang.ProcessBuilder cmd;
            string result = "";

            try
            {
                string[] args = { "/system/bin/cat", "/proc/meminfo" };
                cmd = new ProcessBuilder(args);

                Java.Lang.Process process = cmd.Start();
                using (var inputStream = process.InputStream)
                {
                    byte[] re = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = inputStream.Read(re, bytesRead, re.Length)) != -1)
                    {
                        result += System.Text.Encoding.Default.GetString(re);
                    }
                }
            }
            catch (System.Exception e)
            {
            }
            return result;
        }

        
        private string ReadCPUinfo()
        {
            ProcessBuilder cmd;
            string result = "";

            try
            {
                string[] args = { "/system/bin/cat", "/proc/cpuinfo" };
                cmd = new ProcessBuilder(args);
                Java.Lang.Process process = cmd.Start();
                using (var inputStream = process.InputStream)
                {
                    byte[] re = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = inputStream.Read(re, bytesRead, re.Length)) != -1)
                    {
                        result += System.Text.Encoding.Default.GetString(re);
                    }
                }
            }
            catch (System.Exception ex)
            {
                
            }
            return result;
        }

        public int GetNumberOfCores()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
                return Runtime.GetRuntime().AvailableProcessors();

            return -1;
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