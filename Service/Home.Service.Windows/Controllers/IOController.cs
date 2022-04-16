using Home.Data.Helper;
using Home.Data.Remote;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Web;

namespace Home.Service.Windows.Controllers
{
    [Route("io")]
    [ApiController()]
    public class IOController
    {
        [HttpGet("ls/{path}")]
        public ActionResult Ls(string path)
        {
            try
            {
                path = HttpUtility.UrlDecode(path);

                if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path))
                    return new NotFoundResult();

                var di = new System.IO.DirectoryInfo(path);

                RemoteDirectory root = new RemoteDirectory(path);
                foreach (var item in di.EnumerateDirectories("*.*", System.IO.SearchOption.TopDirectoryOnly))
                    root.Directories.Add(new RemoteDirectory(item.FullName) { LastChange = item.LastWriteTime });
                foreach (var item in di.EnumerateFiles("*.*", System.IO.SearchOption.TopDirectoryOnly))
                    root.Files.Add(new RemoteFile(item.FullName) { Length = item.Length, LastAccessTime = item.LastAccessTime, LastWriteTime = item.LastWriteTimeUtc });

                return new OkObjectResult(root);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("download/{path}")]
        public ActionResult DownloadFile(string path)
        {
            path = HttpUtility.UrlDecode(path);

            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return new NotFoundResult();

            try
            {
                var stream = new System.IO.FileStream(path, System.IO.FileMode.Open);
                return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = System.IO.Path.GetFileName(path) };
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("zip/{path}")]
        public async Task<ActionResult> DownloadDirectoryAsZipFile(string path)
        {
            path = HttpUtility.UrlDecode(path);

            if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path))
                return new NotFoundResult();

            try
            {
                // Get a temp file
                string tempZipFile = System.IO.Path.GetTempFileName();

                // Zip folder to the temp file
                await ZipHelper.CreateZipFileFromDirectoryAsync(path, tempZipFile);

                // Return temp file
                var stream = new System.IO.FileStream(tempZipFile, System.IO.FileMode.Open);
                return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = System.IO.Path.GetFileName(path) + ".zip" };
            }
            catch (Exception)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
