using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using AmalgaDrive.DavServer.Model;
using Microsoft.AspNetCore.Mvc;

namespace AmalgaDrive.DavServer.Controllers
{
    [Route("/dav")]
    public class DavController : Controller
    {
        public DavController(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public IFileSystem FileSystem { get; }

        [HttpOptions]
        [Route("{*url}")]
        public void Options()
        {
            var allows = new[] { "COPY", "DELETE", "GET", "HEAD", "LOCK", "MKCOL", "MOVE", "OPTIONS", "PROPFIND", "PROPPATCH", "PUT", "UNLOCK" };
            Response.Headers.Add("Allow", string.Join(", ", allows));
            Response.Headers.Add("DAV", "1,2,1#extend");
        }

        [HttpGet]
        public void Get()
        {
        }

        [HttpHead]
        public void Head()
        {
        }

        [HttpDelete]
        public void Delete()
        {
        }

        [HttpCopy]
        public void Copy()
        {
        }

        [HttpMkCol]
        public void MkCol()
        {
        }

        [HttpPut]
        public void Put()
        {
        }

        [HttpMove]
        public void Move()
        {
        }

        [HttpLock]
        public void Lock()
        {
        }

        [HttpUnlock]
        public void Unlock()
        {
        }

        [HttpPropFind]
        [Route("{*url}")]
        public IActionResult PropFind(string url = null)
        {
            if (!FileSystem.TryGetItem(url?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), out var info))
                return NotFound();

            string xml;
            using (var stream = Request.Body)
            {
                using (var reader = new StreamReader(Request.Body))
                {
                    xml = reader.ReadToEnd();
                }
            }

            var daveRequest = new XmlDocument();
            if (!string.IsNullOrWhiteSpace(xml))
            {
                daveRequest.LoadXml(xml);
            }
            return Ok();
        }

        [HttpPropPatch]
        public void PropPatch()
        {
        }
    }
}
