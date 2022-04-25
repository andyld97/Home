using Home.Data;
using Home.Data.Helper;
using Home.Data.Remote;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Home.Service.Windows.Controllers
{
    [Route("io")]
    [ApiController()]
    public class IOController
    {
        [HttpPost("ls")]
        public ActionResult Ls([FromBody] RemotePath path)
        {
            try
            {
                if (string.IsNullOrEmpty(path.Path) || !System.IO.Directory.Exists(path.Path))
                    return new NotFoundObjectResult(AnswerExtensions.Fail("Not found"));

                var di = new System.IO.DirectoryInfo(path.Path);

                RemoteDirectory root = new RemoteDirectory(path.Path);
                foreach (var item in di.EnumerateDirectories("*.*", System.IO.SearchOption.TopDirectoryOnly))
                    root.Directories.Add(new RemoteDirectory(item.FullName) { LastChange = item.LastWriteTime });
                foreach (var item in di.EnumerateFiles("*.*", System.IO.SearchOption.TopDirectoryOnly))
                    root.Files.Add(new RemoteFile(item.FullName) { Length = item.Length, LastAccessTime = item.LastAccessTime, LastWriteTime = item.LastWriteTimeUtc });

                return new OkObjectResult(AnswerExtensions.Success(root));
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(AnswerExtensions.Fail(ex.Message));
            }
        }

        [HttpPost("download")]
        public ActionResult DownloadFile([FromBody] RemotePath path)
        {
            if (string.IsNullOrEmpty(path.Path) || !System.IO.File.Exists(path.Path))
                return new NotFoundResult();

            try
            {
                var stream = new System.IO.FileStream(path.Path, System.IO.FileMode.Open);
                return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = System.IO.Path.GetFileName(path.Path) };
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("zip")]
        public async Task<ActionResult> DownloadDirectoryAsZipFile([FromBody] RemotePath path)
        {
            if (string.IsNullOrEmpty(path.Path) || !System.IO.Directory.Exists(path.Path))
                return new NotFoundResult();

            try
            {
                // Get a temp file
                string tempZipFile = System.IO.Path.GetTempFileName();

                // Zip folder to the temp file
                await ZipHelper.CreateZipFileFromDirectoryAsync(path.Path, tempZipFile);

                // Return temp file
                var stream = new System.IO.FileStream(tempZipFile, System.IO.FileMode.Open);
                return new FileStreamResult(stream, "application/octet-stream")
                {
                    FileDownloadName = $"{System.IO.Path.GetFileName(path.Path)}.zip"
                };
            }
            catch (Exception)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
