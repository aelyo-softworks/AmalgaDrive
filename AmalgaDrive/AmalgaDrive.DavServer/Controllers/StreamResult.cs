using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AmalgaDrive.DavServer.Controllers
{
    public class StreamResult : IActionResult
    {
        public StreamResult(Stream stream, string fileName, string contentType)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            if (contentType == null)
                throw new ArgumentNullException(nameof(contentType));

            Stream = stream;
            FileName = fileName;
            ContentType = contentType;
        }

        public Stream Stream { get; }
        public string FileName { get; }
        public string ContentType { get; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            context.HttpContext.Response.ContentType = ContentType;
            context.HttpContext.Response.Headers["Content-Disposition"] = new ContentDispositionHeaderValue("attachment") { Name = FileName }.ToString();

            using (var stream = context.HttpContext.Response.Body)
            {
                await Stream.CopyToAsync(stream);
            }
        }
    }
}