using Home.Data;
using Home.Data.Com;
using Home.Model;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Windows;

namespace Home.Service.Legacy
{
    public class API
    {
        private readonly string url;

        public API(string url)
        {
            this.url = url;
        }

        public bool RegisterDeviceAsync(Device d)
        {
            try
            {
                string url = $"{this.url}/api/v1/device/register";
                var http = (HttpWebRequest)WebRequest.Create(new Uri(url));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(d);
                byte[] bytes = System.Text.Encoding.Default.GetBytes(parsedContent);

                using (System.IO.Stream newStream = http.GetRequestStream())
                {
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();

                    var stream = response.GetResponseStream();
                    var sr = new System.IO.StreamReader(stream);
                    var content = sr.ReadToEnd();

                    var obj = JsonConvert.DeserializeObject<Answer<bool>>(content);
                    if (obj != null && obj.Status == "ok")
                        return true;
                }
            }
            catch (Exception ex)
            {
                // ToDo: Log
            }

            return false;
        }


        public bool SendScreenshotAsync(Screenshot shot)
        {
            try
            {
                string url = $"{this.url}/api/v1/device/screenshot";
                var http = (HttpWebRequest)WebRequest.Create(new Uri(url));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(shot);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(parsedContent);

                using (System.IO.Stream newStream = http.GetRequestStream())
                {
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();

                    var stream = response.GetResponseStream();
                    var sr = new System.IO.StreamReader(stream);
                    var content = sr.ReadToEnd();

                    var obj = JsonConvert.DeserializeObject<Answer<bool>>(content);
                    if (obj != null && obj.Status == "ok")
                        return true;
                }
            }
            catch (Exception ex)
            {
                // ToDo: Log
            }

            return false;
        }

        public Answer<AckResult> SendAckAsync(Device d)
        {
            try
            {
                string url = $"{this.url}/api/v1/device/ack";
                var http = (HttpWebRequest)WebRequest.Create(new Uri(url));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(d);
                // Debug:
                // MessageBox.Show(parsedContent);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(parsedContent);

                using (System.IO.Stream newStream = http.GetRequestStream())
                {
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();

                    var stream = response.GetResponseStream();
                    var sr = new System.IO.StreamReader(stream);
                    var content = sr.ReadToEnd();
                    return JsonConvert.DeserializeObject<Answer<AckResult>>(content);
                }
            }
            catch (WebException we)
            {
                var stream = we.Response?.GetResponseStream();
                var sr = new System.IO.StreamReader(stream);
                var content = sr.ReadToEnd();

                // Debug:
                // MessageBox.Show(content);
                return AnswerExtensions.Fail<AckResult>(content);
            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.ToString());
                return AnswerExtensions.Fail<AckResult>(ex.Message);
            }
        }
    }
}
