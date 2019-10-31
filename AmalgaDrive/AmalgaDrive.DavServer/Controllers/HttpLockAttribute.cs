using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public sealed class HttpLockAttribute : HttpMethodAttribute
    {
        private static readonly string[] _supportedMethods = new[] { "LOCK" };

        public HttpLockAttribute()
            : base(_supportedMethods)
        {
        }

        public HttpLockAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
