using System.Net.Http;
using System.Threading.Tasks;

namespace Home.API
{
    public static class WebHook
    {
        public static async Task NotifyWebHookAsync(string url, string message)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string nUrl = $"{url}{message}";
                    await client.GetAsync(url);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}