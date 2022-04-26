using Home.Data;
using Home.Data.Remote;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Home.Communication
{
    public class RemoteAPI
    {
        private readonly string ip = string.Empty;
        private readonly int port;

        private readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };

        public RemoteAPI(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        /// <summary>
        /// Lists directories and files on the remote machine
        /// </summary>
        /// <param name="remoteDirectory"></param>
        /// <returns></returns>
        public async Task<Answer<RemoteDirectory>> GetRemoteDirectoryAsync(string remoteDirectory)
        {
            try
            {
                string url = $"http://{ip}:{port}/io/ls";
                var result = await httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(new RemotePath(remoteDirectory)), System.Text.Encoding.UTF8, "application/json"));
                var content = await result.Content.ReadAsStringAsync();

                return await System.Text.Json.JsonSerializer.DeserializeAsync<Answer<RemoteDirectory>>(await result.Content.ReadAsStreamAsync());
            }
            catch (Exception ex)
            {
                return AnswerExtensions.Fail<RemoteDirectory>(ex.Message);
            }
        }

        /// <summary>
        /// Downloads the remoteFile and stores it to localFile
        /// </summary>
        /// <param name="remoteFile"></param>
        /// <param name="localFile"></param>
        /// <returns></returns>
        public async Task<Answer<bool>> DownloadFileAsync(string remoteFile, string localFile)
        {
            string url = $"http://{ip}:{port}/io/download";
            return await DownloadFileAsyncImpl(url, remoteFile, localFile);
        }

        /// <summary>
        /// Downloads the remoteFile and returns the stream
        /// </summary>
        /// <param name="remoteFile"></param>
        /// <returns></returns>
        public async Task<Answer<System.IO.Stream>> DownlaodFileAsync(string remoteFile)
        {
            string url = $"http://{ip}:{port}/io/download";
            try
            {
                var result = await httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(new RemotePath(remoteFile)), Encoding.UTF8, "application/json"));
                if (result.IsSuccessStatusCode)
                    return AnswerExtensions.Success(await result.Content.ReadAsStreamAsync());
                else
                    throw new Exception($"Http Status Code: {result.StatusCode}");
            }
            catch (Exception ex)
            {
                return AnswerExtensions.Fail<System.IO.Stream>(ex.Message);
            }
        }

        /// <summary>
        /// Downloads the zip file of the compressed remoteDirectory and stores it to localZipFile
        /// </summary>
        /// <param name="remoteDirectory"></param>
        /// <param name="localZipFile"></param>
        /// <returns></returns>
        public async Task<Answer<bool>> DownloadFolderAsync(string remoteDirectory, string localZipFile)
        {
            string url = $"http://{ip}:{port}/io/zip";
            return await DownloadFileAsyncImpl(url, remoteDirectory, localZipFile); 
        }

        #region Private Methods

        private async Task<Answer<bool>> DownloadFileAsyncImpl(string url, string remotePath, string localFilePath)
        {
            try
            {
                var result = await httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(new RemotePath(remotePath)), Encoding.UTF8, "application/json"));
                if (result.IsSuccessStatusCode)
                {
                    using (System.IO.FileStream fs = new System.IO.FileStream(localFilePath, System.IO.FileMode.Create))
                    {
                        var str = await result.Content.ReadAsStreamAsync();
                        await str.CopyToAsync(fs);
                        return AnswerExtensions.Success(true);
                    }
                }
                else
                    throw new Exception($"Http Status Code: {result.StatusCode}");
            }
            catch (Exception ex)
            {
                return AnswerExtensions.Fail<bool>(ex.Message);
            }
        }

        #endregion
    }
}