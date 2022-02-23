using System.Net.Http;
using System.Threading.Tasks;

namespace Home.Data.Helper
{
    public static class TGLogger
    {
        public static async Task LogTelegram(string message)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://code-a-software.net/bots/home/notice.php?appkey=hujio7824hrt94hbg894gtqe7235lg356&other={message}";
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