using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public sealed class HttpCopyAttribute : HttpMethodAttribute
    {
        private static readonly string[] _supportedMethods = new[] { "COPY" };

        public HttpCopyAttribute()
            : base(_supportedMethods)
        {
        }

        public HttpCopyAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
