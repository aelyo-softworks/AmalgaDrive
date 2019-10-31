using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public sealed class HttpUnlockAttribute : HttpMethodAttribute
    {
        private static readonly string[] _supportedMethods = new[] { "UNLOCK" };

        public HttpUnlockAttribute()
            : base(_supportedMethods)
        {
        }

        public HttpUnlockAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
