using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public sealed class HttpMoveAttribute : HttpMethodAttribute
    {
        private static readonly string[] _supportedMethods = new[] { "MOVE" };

        public HttpMoveAttribute()
            : base(_supportedMethods)
        {
        }

        public HttpMoveAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
