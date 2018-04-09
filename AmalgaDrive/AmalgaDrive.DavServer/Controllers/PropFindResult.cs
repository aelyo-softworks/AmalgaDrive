using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmalgaDrive.DavServer.Controllers
{
    public class PropFindResult : IActionResult
    {
        public PropFindResult(IEnumerable<IFileSystemInfo> infos, ILogger logger, PropFindRequest request)
        {
            if (infos == null)
                throw new ArgumentNullException(nameof(infos));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Infos = infos;
            Logger = logger;
            Request = request;
#if DEBUG
            Debug = true;
#endif
        }

        public IEnumerable<IFileSystemInfo> Infos { get; }
        public ILogger Logger { get; }
        public PropFindRequest Request { get; }
        public bool Debug { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = DavServerExtensions.MultiStatusCode;
            context.HttpContext.Response.ContentType = "text/xml; charset=\"utf-8\"";

            var settings = new XmlWriterSettings();
            settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            settings.Async = true;
            settings.Encoding = Encoding.UTF8;
            settings.ConformanceLevel = ConformanceLevel.Auto;

            Stream stream;
            MemoryStream ms = null;
            if (Debug)
            {
                ms = new MemoryStream();
                stream = ms;
                settings.Indent = true;
            }
            else
            {
                stream = context.HttpContext.Response.Body;
            }

            using (var writer = XmlWriter.Create(stream, settings))
            {
                await writer.WriteStartElementAsync(null, "multistatus", DavServerExtensions.DavNamespaceUri);
                foreach (var info in Infos)
                {
                    await writer.WriteStartElementAsync(null, "response", DavServerExtensions.DavNamespaceUri);
                    var href = info.System.Options.GetPublicUri(context.HttpContext.Request, info);
                    await writer.WriteElementStringAsync(null, "href", DavServerExtensions.DavNamespaceUri, href.ToString());
                    await Request.WriteProperties(writer, info);
                    await writer.WriteEndElementAsync();
                }
                await writer.WriteEndElementAsync();
            }

            if (ms != null)
            {
                Logger?.LogTrace("Response: " + Encoding.UTF8.GetString(ms.ToArray()));
                using (var body = context.HttpContext.Response.Body)
                {
                    ms.Position = 0;
                    await ms.CopyToAsync(body);
                }
            }
        }
    }
}
