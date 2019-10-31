using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public sealed class HttpPropFindAttribute : HttpMethodAttribute
    {
        private static readonly string[] _supportedMethods = new[] { "PROPFIND" };

        public HttpPropFindAttribute()
            : base(_supportedMethods)
        {
        }

        public HttpPropFindAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
