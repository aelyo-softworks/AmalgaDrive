using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmalgaDrive.DavServer.Controllers
{
    public class PropPatchResult : IActionResult
    {
        public PropPatchResult(IFileSystemInfo info, ILogger logger, PropPatchRequest request)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Info = info;
            Logger = logger;
            Request = request;
#if DEBUG
            Debug = true;
#endif
        }

        public IFileSystemInfo Info { get; }
        public ILogger Logger { get; }
        public PropPatchRequest Request { get; }
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
                await writer.WriteStartElementAsync(null, "response", DavServerExtensions.DavNamespaceUri);
                var href = Info.System.Options.GetPublicUri(context.HttpContext.Request, Info);
                await writer.WriteElementStringAsync(null, "href", DavServerExtensions.DavNamespaceUri, href.ToString());

                // updated props
                if (Request.UpdatedProperties.Count > 0)
                {
                    await writer.WriteStartElementAsync(null, "propstat", DavServerExtensions.DavNamespaceUri);
                    foreach (var prop in Request.UpdatedProperties)
                    {
                        await writer.WriteStartElementAsync(null, "prop", DavServerExtensions.DavNamespaceUri);
                        await writer.WriteElementStringAsync(null, prop.LocalName, prop.NamespaceUri, string.Empty);
                        await writer.WriteEndElementAsync(); // prop
                    }
                    await writer.WriteElementStringAsync(null, "status", DavServerExtensions.DavNamespaceUri, "HTTP/1.1 200 OK");
                    await writer.WriteEndElementAsync(); // propstat
                }

                // unknown props
                if (Request.UnknownProperties.Count > 0)
                {
                    await writer.WriteStartElementAsync(null, "propstat", DavServerExtensions.DavNamespaceUri);
                    foreach (var prop in Request.UnknownProperties)
                    {
                        await writer.WriteStartElementAsync(null, "prop", DavServerExtensions.DavNamespaceUri);
                        await writer.WriteElementStringAsync(null, prop.LocalName, prop.NamespaceUri, string.Empty);
                        await writer.WriteEndElementAsync(); // prop
                    }
                    await writer.WriteElementStringAsync(null, "status", DavServerExtensions.DavNamespaceUri, "HTTP/1.1 404 Not Found");
                    await writer.WriteEndElementAsync(); // propstat
                }

                // denied props
                if (Request.UnauthorizedProperties.Count > 0)
                {
                    await writer.WriteStartElementAsync(null, "propstat", DavServerExtensions.DavNamespaceUri);
                    foreach (var prop in Request.UnauthorizedProperties)
                    {
                        await writer.WriteStartElementAsync(null, "prop", DavServerExtensions.DavNamespaceUri);
                        await writer.WriteElementStringAsync(null, prop.LocalName, prop.NamespaceUri, string.Empty);
                        await writer.WriteEndElementAsync(); // prop
                    }

                    await writer.WriteElementStringAsync(null, "status", DavServerExtensions.DavNamespaceUri, "HTTP/1.1 401 Unauthorized");
                    await writer.WriteEndElementAsync(); // propstat
                }

                await writer.WriteEndElementAsync(); // response
                await writer.WriteEndElementAsync(); // multistatus
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
