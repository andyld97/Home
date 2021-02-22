using Home.Data;
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
        public static readonly string REGISTER = "register";
        public static readonly string ACK = "ack";
        public static readonly string UPDATE = "update";

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
                return false;
            }
        }

        public async Task<string> SendAckAsync(Device device)
        {
            try
            {
                string url = GenerateEpUrl(false, ACK);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var answer = System.Text.Json.JsonSerializer.Deserialize<Answer<object>>(content);
                    if (answer != null && answer.Status == "ok")
                        return string.Empty;
                    else
                        return answer.ErrorMessage;
                }
                else
                    return string.Empty;


            }
            catch (Exception ex)
            {
                // LOG
                return ex.Message;
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
                    item.Success = (item.Status == "ok");
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


        public string GenerateEpUrl(bool communication, string ep)
        {
            string controller = (communication ? COMMUNICATION_C : DEVICE_C);
            return $"{string.Format(BASE_URL, host)}{controller}/{ep}";
        }
    }
}

