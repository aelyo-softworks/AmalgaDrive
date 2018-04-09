using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace AmalgaDrive.DavServer.Controllers
{
    public class DavController : Controller
    {
        public DavController(IFileSystem fileSystem, ILogger<DavController> logger)
        {
            FileSystem = fileSystem;
            Logger = logger;
            var x = HttpContext;
        }

        public IFileSystem FileSystem { get; }
        public ILogger<DavController> Logger { get; }

        private void Log(string text, [CallerMemberName] string methodName = null) => Logger.LogInformation(Thread.CurrentThread.ManagedThreadId + ":" + methodName + ": " + text);

        private bool CheckUrl(string url) => CheckUrl(url, out string relativeUrl);
        private bool CheckUrl(string url, out string relativePath)
        {
            relativePath = null;
            if (url == null)
                return string.IsNullOrEmpty(FileSystem.Options.BaseUrl);

            if (url.EqualsIgnoreCase(FileSystem.Options.BaseUrl))
                return true;

            if (string.IsNullOrWhiteSpace(FileSystem.Options.BaseUrl))
            {
                relativePath = url.Replace("//", @"\");
                return true;
            }

            string dir = FileSystem.Options.BaseUrl + "/";
            if (url.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = url.Replace("//", "/").Substring(dir.Length).Replace("/", @"\");
                return true;
            }
            return false;
        }

        [HttpOptions]
        [Route("{*url}")]
        public IActionResult Options(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url))
                return NotFound();

            var allows = new[] { "COPY", "DELETE", "GET", "HEAD", "LOCK", "MKCOL", "MOVE", "OPTIONS", "PROPFIND", "PROPPATCH", "PUT", "UNLOCK" };
            Response.Headers.Add("Allow", string.Join(", ", allows));
            Response.Headers.Add("DAV", "1,2,1#extend");
            return Ok();
        }

        [HttpGet]
        [Route("{*url}")]
        public IActionResult Get(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            Log("RelativePath: " + relativePath);
            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            if (!(info is IFileInfo file))
                return NoContent();

            var stream = file.OpenRead();
            if (stream == null)
                return NoContent();

            return new StreamResult(stream, file.Name, file.GetContentType());
        }

        [HttpDelete]
        [Route("{*url}")]
        public IActionResult Delete(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            if (info is IDirectoryInfo dir)
            {
                if (dir.IsRoot)
                    return Unauthorized();
            }

            info.Delete();
            return Ok();
        }

        [HttpCopy]
        [Route("{*url}")]
        public IActionResult Copy(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            return Ok();
        }

        [HttpMkCol]
        [Route("{*url}")]
        public IActionResult MkCol(string url = null)
        {
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            if (FileSystem.TryGetItem(relativePath, out var info))
            {
                if (info is IDirectoryInfo)
                    return Ok();

                return StatusCode((int)HttpStatusCode.MethodNotAllowed);
            }

            Log("Url: " + url);
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest();

            string name;
            string dirUrl;
            int pos = url.LastIndexOf('/');
            if (pos <= 0)
            {
                name = url;
                dirUrl = null;
            }
            else
            {
                name = url.Substring(pos + 1);
                dirUrl = url.Substring(0, pos);
            }

            if (!CheckUrl(dirUrl, out var dirRelativePath))
                return NotFound();

            if (!FileSystem.TryGetItem(dirRelativePath, out var dirInfo))
                return NotFound();

            if (!(dirInfo is IDirectoryInfo dir))
                return NotFound();

            dir.Create(name);
            return StatusCode((int)HttpStatusCode.Created);
        }

        [HttpPut]
        [Route("{*url}")]
        public IActionResult Put(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            return Ok();
        }

        [HttpMove]
        [Route("{*url}")]
        public IActionResult Move(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            var overwrite = Request.Headers["Overwrite"].ToString().EqualsIgnoreCase("t");
            var destinationUrl = Request.Headers["Destination"].ToString();
            if (string.IsNullOrWhiteSpace(destinationUrl))
                return BadRequest();

            if (destinationUrl.EndsWith("/"))
            {
                destinationUrl = destinationUrl.Substring(0, destinationUrl.Length - 1);
            }
            var destination = new Uri(destinationUrl);
            if (!FileSystem.Options.TryGetRelativePath(Request, destination.ToString(), out var relativeDestinationPath))
                return Unauthorized();

            Log("RelativePath: " + relativePath);
            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            if (FileSystem.TryGetItem(relativeDestinationPath, out var destinationInfo))
            {
                if (!overwrite)
                    return StatusCode((int)HttpStatusCode.PreconditionFailed);

                destinationInfo.Delete();
            }

            info.MoveTo(relativeDestinationPath);
            return Ok();
        }

        [HttpLock]
        [Route("{*url}")]
        public IActionResult Lock(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            return Ok();
        }

        [HttpUnlock]
        [Route("{*url}")]
        public IActionResult Unlock(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            return Ok();
        }

        [HttpPropFind]
        [Route("{*url}")]
        public IActionResult PropFind(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            Log("RelativePath: " + relativePath);
            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            string xml;
            using (var stream = Request.Body)
            {
                using (var reader = new StreamReader(Request.Body))
                {
                    xml = reader.ReadToEnd();
                    Log("Xml: " + xml);
                }
            }

            var doc = new XmlDocument();
            if (!string.IsNullOrWhiteSpace(xml))
            {
                doc.LoadXml(xml);
            }

            var dr = new DavRequest(doc);
            if (dr.AllProperties && dr.AllPropertiesNames)
                return BadRequest();

            var depth = Request.GetDepth();
            Log("Depth: " + depth);
            return new MultiStatusResult(info.EnumerateFileSystemInfo(depth), Logger, dr);
        }

        [HttpPropPatch]
        [Route("{*url}")]
        public IActionResult PropPatch(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            return Ok();
        }
    }
}
