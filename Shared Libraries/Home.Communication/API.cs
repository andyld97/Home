using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Home.Communication
{
    public class API
    {
        private readonly string host = string.Empty;

        public static readonly HttpClient httpClient = new HttpClient();
        public static readonly string BASE_URL = "{0}/api/v1/";
        public static readonly string COMMUNICATION_C = "communication";
        public static readonly string DEVICE_C = "device";

        public static readonly string LOGIN = "login";
        public static readonly string LOGOFF = "logoff";
        public static readonly string REGISTER = "register";
        public static readonly string ACK = "ack";
        public static readonly string UPDATE = "update";
        public static readonly string SCREENSHOT = "screenshot";
        public static readonly string GET_SCREENSHOT = "get_screenshot";
        public static readonly string RECIEVE_SCREENSHOT = "recieve_screenshot";
        public static readonly string CLEAR_LOG = "clear_log";

        public API(string host)
        {
            this.host = host;
        }

        public async Task<Answer<List<Device>>> LoginAsync(Client client)
        {
            try
            {
                string url = GenerateEpUrl(true, LOGIN);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(client), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var answer = System.Text.Json.JsonSerializer.Deserialize<Answer<List<Device>>>(content);
                    if (answer != null && answer.Status == "ok")
                        return AnswerExtensions.Success(answer.Result);
                    else
                        return AnswerExtensions.Fail<List<Device>>(answer.ErrorMessage);
                }
                else
                    return AnswerExtensions.Fail<List<Device>>("Invalid answer recieved!");

            }
            catch (Exception ex)
            {
                return AnswerExtensions.Fail<List<Device>>(ex.Message);
            }
        }

        public async Task<Answer<string>> LogoffAsync(Client client)
        {
            try
            {
                string url = GenerateEpUrl(true, LOGOFF);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(client), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var answer = System.Text.Json.JsonSerializer.Deserialize<Answer<string>>(content);
                    if (answer != null && answer.Status == "ok")
                        return AnswerExtensions.Success(answer.Result);
                    else
                        return AnswerExtensions.Fail<string>(answer.ErrorMessage);
                }
                else
                    return AnswerExtensions.Fail<string>("Invalid answer recieved!");

            }
            catch (Exception ex)
            {
                return AnswerExtensions.Fail<string>(ex.Message);
            }
        }


        public async Task<bool> RegisterDeviceAsync(Device device)
        {
            try
            {
                string url = GenerateEpUrl(false, REGISTER);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var answer = System.Text.Json.JsonSerializer.Deserialize<Answer<object>>(content);
                    if (answer != null && answer.Status == "ok")
                        return true;
                    else
                        return false;
                }
                else
                    return false;


            }
            catch (Exception ex)
            {
                // LOG
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<Answer<AckResult>> SendAckAsync(Device device)
        {
            try
            {
                string url = GenerateEpUrl(false, ACK);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                    return System.Text.Json.JsonSerializer.Deserialize<Answer<AckResult>>(content);
                else
                    return AnswerExtensions.Fail<AckResult>("Recieved empty string!");

            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<AckResult>(ex.Message);
            }
        }

        public async Task<Answer<EventQueueItem>> UpdateAsync(Client client)
        {
            try
            {
                string url = GenerateEpUrl(true, UPDATE);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(client), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var item = System.Text.Json.JsonSerializer.Deserialize<Answer<EventQueueItem>>(content);
                    return item;
                }
                else
                    return AnswerExtensions.Fail<EventQueueItem>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<EventQueueItem>(ex.Message);
            }
        }


        public async Task<Answer<bool>> SendScreenshotAsync(Screenshot screenshot)
        {
            try
            {
                string url = GenerateEpUrl(false, SCREENSHOT);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(screenshot), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var item = System.Text.Json.JsonSerializer.Deserialize<Answer<bool>>(content);
                    return item;
                }
                else
                    return AnswerExtensions.Fail<bool>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<bool>(ex.Message);
            }
        }

        public async Task<Answer<string>> SendMessageAsync(Message message)
        {
            try
            {
                string url = GenerateEpUrl(true, "send_message");
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(message), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var item = System.Text.Json.JsonSerializer.Deserialize<Answer<string>>(content);
                    return item;
                }
                else
                    return AnswerExtensions.Fail<string>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<string>(ex.Message);
            }
        }

        public async Task<Answer<Screenshot>> RecieveScreenshotAsync(Device device, string fileName)
        {
            try
            {
                string url = $"{GenerateEpUrl(true, RECIEVE_SCREENSHOT)}/{device.ID}/{fileName}";
                var result = await httpClient.GetAsync(url); 

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var item = System.Text.Json.JsonSerializer.Deserialize<Answer<Screenshot>>(content);
                    return item;
                }
                else
                    return AnswerExtensions.Fail<Screenshot>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<Screenshot>(ex.Message);
            }
        }

        public async Task<Answer<bool>> AquireScreenshotAsync(Client client, Device device)
        {
            try
            {
                string url = $"{GenerateEpUrl(true, GET_SCREENSHOT)}/{client.ID}/{device.ID}";
                var result = await httpClient.GetAsync(url);

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var item = System.Text.Json.JsonSerializer.Deserialize<Answer<bool>>(content);
                    return item;
                }
                else
                    return AnswerExtensions.Fail<bool>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<bool>(ex.Message);
            }
        }

        public async Task<Answer<string>> ClearDeviceLogAsync(Device device)
        {
            try
            {
                string url = $"{GenerateEpUrl(true, CLEAR_LOG)}/{device.ID}";
                var result = await httpClient.GetAsync(url);

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var item = System.Text.Json.JsonSerializer.Deserialize<Answer<string>>(content);
                    return item;
                }
                else
                    return AnswerExtensions.Fail<string>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<string>(ex.Message);
            }
        }

        public string GenerateEpUrl(bool communication, string ep)
        {
            string controller = (communication ? COMMUNICATION_C : DEVICE_C);
            return $"{string.Format(BASE_URL, host)}{controller}/{ep}";
        }
    }
}