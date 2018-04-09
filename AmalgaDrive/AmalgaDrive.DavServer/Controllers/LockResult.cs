using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;

namespace AmalgaDrive.DavServer.Controllers
{
    public class LockResult : IActionResult
    {
        public LockResult(string lockToken)
        {
            LockToken = lockToken;
        }

        public string LockToken { get; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            context.HttpContext.Response.ContentType = "text/xml; charset=\"utf-8\"";

            var settings = new XmlWriterSettings();
            settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            settings.Async = true;
            settings.Encoding = Encoding.UTF8;
            settings.ConformanceLevel = ConformanceLevel.Auto;

            using (var stream = context.HttpContext.Response.Body)
            using (var writer = XmlWriter.Create(stream, settings))
            {
                await writer.WriteStartElementAsync(null, "prop", DavServerExtensions.DavNamespaceUri);
                await writer.WriteStartElementAsync(null, "lockdiscovery", DavServerExtensions.DavNamespaceUri);
                await writer.WriteStartElementAsync(null, "activelock", DavServerExtensions.DavNamespaceUri);
                await writer.WriteStartElementAsync(null, "locktoken", DavServerExtensions.DavNamespaceUri);
                await writer.WriteStringAsync(LockToken);
                await writer.WriteEndElementAsync();
                await writer.WriteEndElementAsync();
                await writer.WriteEndElementAsync();
                await writer.WriteEndElementAsync();
            }
        }
    }
}
