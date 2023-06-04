using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home
{
    public class HomeConsts
    {
        public static readonly string CACHE_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Home", "Cache");
        public static readonly string WEBVIEW_CACHE_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Home", "WebView2Cache");
    }
}
