using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public class HttpLockAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "LOCK" };

        public HttpLockAttribute() : base(_supportedMethods)
        {
        }

        public HttpLockAttribute(string template) : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
