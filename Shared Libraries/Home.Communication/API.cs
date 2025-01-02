// Ignore Spelling: WOL LOGOFF ACK HW

using Home.Data;
using Home.Data.Com;
using Home.Data.Events;
using Home.Data.Helper;
using Home.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using static Home.Model.Device;

namespace Home.Communication
{
    public class API
    {
        private readonly string host = string.Empty;

        public static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10)  };
        public static readonly string BASE_URL = "{0}/api/v1/";
        public static readonly string COMMUNICATION_C = "communication";
        public static readonly string DEVICE_C = "device";
        public static readonly string WOL_C = "wol";

        public static readonly string LOGIN = "login";
        public static readonly string LOGOFF = "logoff";
        public static readonly string REGISTER = "register";
        public static readonly string ACK = "ack";
        public static readonly string UPDATE = "update";
        public static readonly string SCREENSHOT = "screenshot";
        public static readonly string GET_SCREENSHOT = "get_screenshot";
        public static readonly string RECEIVE_SCREENSHOT = "recieve_screenshot";
        public static readonly string CLEAR_LOG = "clear_log";
        public static readonly string CLEAR_HW_CHANGES = "clear_hw_changes";
        public static readonly string SEND_MESSAGE = "send_message";
        public static readonly string SEND_COMMAND = "send_command";
        public static readonly string BROADCAST_SHUTDOWN = "broadcast_shutdown";
        public static readonly string STATUS = "status";
        public static readonly string DELETE = "delete";
        public static readonly string TEST = "test";
        public static readonly string SchedulingRules = "SchedulingRules";
        public static readonly string UpdateSchedulingRulesEP = "UpdateSchedulingRules";
        public static readonly string SendWakeUpRequest = "SendWakeUpRequest";

        public API(string host)
        {
            this.host = host;
        }

        public async Task<Answer<List<Device>>> LoginAsync(Client client)
        {
            try
            { 
                string url = GenerateEpUrl(Endpoint.Communication, LOGIN);
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
                    return AnswerExtensions.Fail<List<Device>>("Invalid answer received!");
            }
            catch (Exception ex)
            {
                return AnswerExtensions.Fail<List<Device>>(ex.Message);
            }
        }

        public async Task<(bool, string)> TestConnectionAsync()
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.Communication, TEST);
                var result = await httpClient.GetAsync(url);

                if (result.IsSuccessStatusCode)
                    return (true, string.Empty);
                else
                    throw new Exception($"Http Status Code: {result.StatusCode}");

            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<Answer<string>> LogoffAsync(Client client)
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.Communication, LOGOFF);
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
                    return AnswerExtensions.Fail<string>("Invalid answer received!");

            }
            catch (Exception ex)
            {
                return AnswerExtensions.Fail<string>(ex.Message);
            }
        }

        public async Task<(bool, string)> RegisterDeviceAsync(Device device)
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.Device, REGISTER);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var answer = System.Text.Json.JsonSerializer.Deserialize<Answer<object>>(content);
                    if (answer != null && answer.Status == "ok")
                        return (true, string.Empty);
                    else
                        throw new Exception(answer?.ErrorMessage);
                }
                else
                    return (false, string.Empty);
            }
            catch (Exception ex)
            {                
                return (false, ex.Message);
            }
        }

        public async Task<Answer<AckResult>> SendAckAsync(Device device)
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.Device, ACK);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                    return System.Text.Json.JsonSerializer.Deserialize<Answer<AckResult>>(content);
                else
                    return AnswerExtensions.Fail<AckResult>("Received empty string!");

            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<AckResult>(ex.Message);
            }
        }

        public async Task<Answer<List<EventQueueItem>>> UpdateAsync(Client client)
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.Communication, UPDATE);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(client), System.Text.Encoding.UTF8, "application/json"));

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Answer<List<EventQueueItem>>>(content);
                }
                else
                    return AnswerExtensions.Fail<List<EventQueueItem>>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<List<EventQueueItem>>(ex.Message);
            }
        }


        public async Task<Answer<bool>> SendScreenshotAsync(Screenshot screenshot)
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.Device, SCREENSHOT);
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
                string url = GenerateEpUrl(Endpoint.Communication, SEND_MESSAGE);
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

        public async Task<Answer<string>> ShutdownOrRestartDeviceAsync(bool shutdown, Device device)
        {
            if (device.OS == OSType.Android)
                return AnswerExtensions.Fail<string>("Android devices doesn't support this command!");

            string parameter = string.Empty;
            string executable;
            if (device.OS.IsLinux() || device.OS == OSType.Unix || device.OS == OSType.Other)
            {
                if (shutdown)
                {
                    executable = "shutdown";
                    parameter = "-h now";
                }
                else
                    executable = "reboot";
            }
            else
            {
                executable = "shutdown.exe";
                parameter = $"/{(shutdown ? "s" : "r")} /f /t 00";
            }

            return await SendCommandAsync(new Data.Com.Command() { DeviceID = device.ID, Executable = executable, Parameter = parameter });
        }

        public async Task<Answer<bool>> WakeOnLanAsync(Device d)
        {
            return await WakeOnLanAsync(d.MacAddress);
        }

        public async Task<Answer<bool>> WakeOnLanAsync(string macAddress)
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.WOL, SendWakeUpRequest);
                var result = await httpClient.GetAsync($"{url}/{macAddress}/9");

                result.EnsureSuccessStatusCode();

                return AnswerExtensions.Success<bool>(true);
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<bool>(ex.Message);
            }
        }

        public async Task<Answer<string>> SendCommandAsync(Command command)
        {
            try
            {
                string url = GenerateEpUrl(Endpoint.Communication, SEND_COMMAND);
                var result = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(command), System.Text.Encoding.UTF8, "application/json"));

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

        public async Task<byte[]> ReceiveScreenshotAsync(Device device, string fileName)
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, RECEIVE_SCREENSHOT)}/{device.ID}/{fileName}";
                var result = await httpClient.GetAsync(url); 

                var content = await result.Content.ReadAsByteArrayAsync();
                return content;
            }
            catch (Exception)
            {
                // LOG
                return null;
            }
        }

        public async Task<bool> DownloadScreenshotToCache(Device device, string cachePath, string fileName = "")
        {
            if (device.OS == OSType.Android)
                return true;

            string cacheDevicePath = System.IO.Path.Combine(cachePath, device.ID);
            if (!System.IO.Directory.Exists(cacheDevicePath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(cacheDevicePath);
                }
                catch
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                if (device.Screenshots.Any())
                {
                    string lastFileName = device.Screenshots.LastOrDefault(x => x.ScreenIndex == null)?.Filename;
                    if (string.IsNullOrEmpty(lastFileName))
                        lastFileName = device.Screenshots.LastOrDefault()?.Filename;

                    string path = System.IO.Path.Combine(cacheDevicePath, lastFileName + ".png");

                    if (System.IO.File.Exists(path))
                        return true;

                    fileName = lastFileName;
                }
            }

            if (string.IsNullOrEmpty(fileName))
                return true;

            // Download latest screenshot
            var result = await ReceiveScreenshotAsync(device, fileName);
            if (result != null)
            {
                string path = System.IO.Path.Combine(cacheDevicePath, fileName + ".png");
                try
                {
                    System.IO.File.WriteAllBytes(path, result);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<Answer<bool?>> AquireScreenshotAsync(Client client, Device device)
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, GET_SCREENSHOT)}/{client.ID}/{device.ID}";
                var result = await httpClient.GetAsync(url);

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var item = System.Text.Json.JsonSerializer.Deserialize<Answer<bool?>>(content, new System.Text.Json.JsonSerializerOptions() {  });
                    return item;
                }
                else
                    return AnswerExtensions.Fail<bool?>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<bool?>(ex.Message);
            }
        }

        public async Task<Answer<string>> SetLiveStatusAsync(Client client, Device device , bool status)
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, STATUS)}/{client.ID}/{device.ID}/{status}";
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

        public async Task<Answer<string>> DeleteDeviceAsync(Device device)
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, DELETE)}/{device.ID}";
                var result = await httpClient.DeleteAsync(url);

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

        public async Task<Answer<string>> ClearDeviceLogAsync(Device device)
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, CLEAR_LOG)}/{device.ID}";
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

        public async Task<Answer<string>> ClearDeviceHardwareChangesAsync(Device device)
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, CLEAR_HW_CHANGES)}/{device.ID}";
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

        public async Task<Answer<bool>> BroadcastShutdownAsync(Client client, string code, string reason, bool shutdown_all)
        {
            HttpStatusCode? statusCode = null;
            try
            {
                string url = GenerateEpUrl(Endpoint.Communication, BROADCAST_SHUTDOWN);
                var result = await httpClient.GetAsync($"{url}?code={UrlEncoder.Default.Encode(code)}&reason={UrlEncoder.Default.Encode(reason)}&shutdown_all={shutdown_all}");
                statusCode = result.StatusCode;
                result.EnsureSuccessStatusCode();

                return AnswerExtensions.Success(true);
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                switch (statusCode)
                {
                    case HttpStatusCode.NotFound: message = "No devices found to shutdown!"; break;
                    case HttpStatusCode.Unauthorized: message = "Wrong code used!"; break;
                    case HttpStatusCode.Forbidden: message = "API config does not allow broadcast shutdown!"; break;
                }

                return AnswerExtensions.Fail<bool>(message);                
            }
        }

        #region Device Scheduling Rules

        public async Task<Answer<IEnumerable<DeviceSchedulingRule>>> GetSchedulingRulesAsync()
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, SchedulingRules)}";
                var result = await httpClient.GetAsync(url);

                if (result.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return AnswerExtensions.Success((IEnumerable<DeviceSchedulingRule>)new DeviceSchedulingRule[0]);

                var content = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                    return AnswerExtensions.Success(System.Text.Json.JsonSerializer.Deserialize<IEnumerable<DeviceSchedulingRule>>(content));
                else
                    return AnswerExtensions.Fail<IEnumerable<DeviceSchedulingRule>>("Empty content!");
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<IEnumerable<DeviceSchedulingRule>>(ex.Message);
            }
        }

        public async Task<Answer<bool>> UpdateSchedulingRules(IEnumerable<DeviceSchedulingRule> rules)
        {
            try
            {
                string url = $"{GenerateEpUrl(Endpoint.Communication, UpdateSchedulingRulesEP)}";
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rules), System.Text.Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(url, content);

                result.EnsureSuccessStatusCode();

                return AnswerExtensions.Success(true);
            }
            catch (Exception ex)
            {
                // LOG
                return AnswerExtensions.Fail<bool>(ex.Message);
            }
        }

        #endregion

        public string GenerateEpUrl(Endpoint mode, string ep)
        {
            string controller = string.Empty;

            if (mode == Endpoint.Communication)
                controller = COMMUNICATION_C;
            else if (mode == Endpoint.Device)
                controller = DEVICE_C;
            else if (mode == Endpoint.WOL)
                controller = WOL_C;

            return $"{string.Format(BASE_URL, host)}{controller}/{ep}";
        }

        public enum Endpoint
        {
            Communication,
            Device,
            WOL
        }
    }
}