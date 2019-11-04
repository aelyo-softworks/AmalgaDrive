using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmalgaDrive.DavServer.Controllers
{
    public class DavController : Controller
    {
        // this constructor will implicitely call DavServerExtensions's AddTransient code
        public DavController(IFileSystem fileSystem, ILogger<DavController> logger)
        {
            FileSystem = fileSystem;
            Logger = logger;
        }

        public IFileSystem FileSystem { get; }
        public ILogger<DavController> Logger { get; }

        private async Task DumpRequestDocument([CallerMemberName] string methodName = null) => _ = await GetRequestDocument(methodName);

        private async Task<XmlDocument> GetRequestDocument([CallerMemberName] string methodName = null)
        {
            string xml;
            using (var stream = Request.Body)
            {
                using (var reader = new StreamReader(Request.Body))
                {
                    xml = await reader.ReadToEndAsync();
                    Log("Xml: " + xml, methodName);
                }
            }

            var doc = new XmlDocument();
            if (!string.IsNullOrWhiteSpace(xml))
            {
                doc.LoadXml(xml);
            }
            return doc;
        }

        private void Log(string text, [CallerMemberName] string methodName = null) => Logger.LogInformation(Thread.CurrentThread.ManagedThreadId + ":" + methodName + ": " + text);

        private bool CheckUrl(string url) => CheckUrl(url, out _);
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
        public async Task<IActionResult> Options(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
            if (!CheckUrl(url))
                return NotFound();

            var allows = new[] { "COPY", "DELETE", "GET", "HEAD", "LOCK", "MKCOL", "MOVE", "OPTIONS", "PROPFIND", "PROPPATCH", "PUT", "UNLOCK" };
            Response.Headers.Add("Allow", string.Join(", ", allows));
            Response.Headers.Add("DAV", "1,2,1#extend");
            return Ok();
        }

        [HttpHead]
        [Route("{*url}")]
        public async Task<IActionResult> Head(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            return new StreamResult(null, info.Name, info.GetContentType());
        }

        [HttpGet]
        [Route("{*url}")]
        public async Task<IActionResult> Get(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

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
        public async Task<IActionResult> Delete(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            if (info is IDirectoryInfo dir)
            {
                if (dir.IsRoot)
                    return Unauthorized();
            }

            if (info.Attributes.HasFlag(FileAttributes.ReadOnly))
                return Unauthorized();

            try
            {
                info.Delete();
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpCopy]
        [Route("{*url}")]
        public async Task<IActionResult> Copy(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
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
            
            var destination = new Uri(destinationUrl, UriKind.RelativeOrAbsolute);
            if (!FileSystem.Options.TryGetRelativePath(Request, destination.ToString(), out var relativeDestinationPath))
                return Unauthorized();

            Log("RelativePath: " + relativePath);
            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            try
            {
                if (FileSystem.TryGetItem(relativeDestinationPath, out var destinationInfo))
                {
                    if (!overwrite)
                        return StatusCode((int)HttpStatusCode.PreconditionFailed);

                    destinationInfo.Delete();
                }

                info.CopyTo(relativeDestinationPath, overwrite);
                return StatusCode((int)HttpStatusCode.Created);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpMkCol]
        [Route("{*url}")]
        public async Task<IActionResult> MkCol(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            if (FileSystem.TryGetItem(relativePath, out var info))
            {
                if (info is IDirectoryInfo)
                    return Ok();

                return StatusCode((int)HttpStatusCode.MethodNotAllowed);
            }

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

            try
            {
                dir.Create(name);
                return StatusCode((int)HttpStatusCode.Created);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpPut]
        [Route("{*url}")]
        [DisableRequestSizeLimit] // note we added this to disable the 30M request limit https://github.com/aspnet/Announcements/issues/267
        public async Task<IActionResult> Put(string url = null)
        {
            Log("Url: " + url);
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            if (FileSystem.TryGetItem(relativePath, out var info))
            {
                if (info is IDirectoryInfo)
                    return StatusCode((int)HttpStatusCode.MethodNotAllowed);
            }

            string path = Path.Combine(FileSystem.RootPath, relativePath);
            try
            {
                Extensions.EnsureFileDirectory(path);
                using (var stream = System.IO.File.OpenWrite(path))
                {
                    // note this can be empty, in this case we create an empty file
                    using (var body = Request.Body)
                    {
                        await body.CopyToAsync(stream);
                    }
                }
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpMove]
        [Route("{*url}")]
        public async Task<IActionResult> Move(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
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

            var destination = new Uri(destinationUrl, UriKind.RelativeOrAbsolute);
            if (!FileSystem.Options.TryGetRelativePath(Request, destination.ToString(), out var relativeDestinationPath))
                return Unauthorized();

            Log("RelativePath: " + relativePath);
            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            try
            {
                if (FileSystem.TryGetItem(relativeDestinationPath, out var destinationInfo))
                {
                    if (!overwrite)
                        return StatusCode((int)HttpStatusCode.PreconditionFailed);

                    destinationInfo.Delete();
                }

                info.MoveTo(relativeDestinationPath);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpLock]
        [Route("{*url}")]
        public async Task<IActionResult> Lock(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
            if (!CheckUrl(url))
                return NotFound();

            // this is fake of course, we don't really support locking
            var token = Guid.NewGuid().ToString("N");
            return new LockResult(token);
        }

        [HttpUnlock]
        [Route("{*url}")]
        public async Task<IActionResult> Unlock(string url = null)
        {
            Log("Url: " + url);
            await DumpRequestDocument();
            if (!CheckUrl(url))
                return NotFound();

            // that's ok, we don't really support locking
            return Ok();
        }

        [HttpPropFind]
        [Route("{*url}")]
        public async Task<IActionResult> PropFind(string url = null)
        {
            Log("Url: " + url);
            var doc = await GetRequestDocument();
            var dr = new PropFindRequest(doc);
            if (dr.AllProperties && dr.AllPropertiesNames)
                return BadRequest();

            // special handling for windows miniredir and other who keep asking for root...
            if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(FileSystem.Options.BaseUrl))
                return NoContent();
            
            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            Log("RelativePath: " + relativePath);
            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            var depth = Request.GetDepth();
            Log("Depth: " + depth);
            return new PropFindResult(info.EnumerateFileSystemInfo(depth), Logger, dr);
        }

        [HttpPropPatch]
        [Route("{*url}")]
        public async Task<IActionResult> PropPatch(string url = null)
        {
            Log("Url: " + url);
            var doc = await GetRequestDocument();
            var dr = new PropPatchRequest(doc);

            if (!CheckUrl(url, out var relativePath))
                return NotFound();

            Log("RelativePath: " + relativePath);
            if (!FileSystem.TryGetItem(relativePath, out var info))
                return NotFound();

            dr.Update(info);
            return new PropPatchResult(info, Logger, dr);
        }
    }
}
