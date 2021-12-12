using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;

namespace Home.Service.Android
{
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