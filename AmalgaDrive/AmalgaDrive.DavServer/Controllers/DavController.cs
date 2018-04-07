using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmalgaDrive.DavServer.Controllers
{
    [Route("/dav")]
    public class DavController : Controller
    {
        public DavController(IFileSystem fileSystem, ILogger<DavController> logger)
        {
            FileSystem = fileSystem;
            Logger = logger;
        }

        public IFileSystem FileSystem { get; }
        public ILogger<DavController> Logger { get; }

        private void Log(string text, [CallerMemberName] string methodName = null) => Logger.LogInformation(Thread.CurrentThread.ManagedThreadId + ":" + methodName + ": " + text);

        [HttpOptions]
        [Route("{*url}")]
        public void Options()
        {
            Log("");
            var allows = new[] { "COPY", "DELETE", "GET", "HEAD", "LOCK", "MKCOL", "MOVE", "OPTIONS", "PROPFIND", "PROPPATCH", "PUT", "UNLOCK" };
            Response.Headers.Add("Allow", string.Join(", ", allows));
            Response.Headers.Add("DAV", "1,2,1#extend");
        }

        [HttpGet]
        public void Get()
        {
            Log(string.Empty);
        }

        [HttpHead]
        public void Head()
        {
            Log(string.Empty);
        }

        [HttpDelete]
        [Route("{*url}")]
        public IActionResult Delete(string url = null)
        {
            Log("Url: " + url);
            if (!FileSystem.TryGetItem(url?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), out var info))
                return NotFound();

            return Ok();
        }

        [HttpCopy]
        public void Copy()
        {
            Log(string.Empty);
        }

        [HttpMkCol]
        public void MkCol()
        {
            Log(string.Empty);
        }

        [HttpPut]
        public void Put()
        {
            Log(string.Empty);
        }

        [HttpMove]
        public void Move()
        {
            Log(string.Empty);
        }

        [HttpLock]
        public void Lock()
        {
            Log(string.Empty);
        }

        [HttpUnlock]
        public void Unlock()
        {
            Log(string.Empty);
        }

        [HttpPropFind]
        [Route("{*url}")]
        public IActionResult PropFind(string url = null)
        {
            Log("Url: " + url);
            if (!FileSystem.TryGetItem(url?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), out var info))
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
            return new MultiStatusResult(info.EnumerateFileSysteminfo(depth), Logger, dr);
        }

        [HttpPropPatch]
        public void PropPatch()
        {
            Log(string.Empty);
        }

        [Route("/info")]
        public Info GetInfo()
        {
            Log(string.Empty);
            return new Info();
        }

        [DataContract(Name = "Info", Namespace = "")]
        public class Info
        {
            public Info()
            {
                Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0.0";
                UtcNow = DateTime.UtcNow;
            }

            [DataMember]
            public string Version { get; set; }

            [DataMember]
            public DateTime UtcNow { get; set; }
        }
    }
}
