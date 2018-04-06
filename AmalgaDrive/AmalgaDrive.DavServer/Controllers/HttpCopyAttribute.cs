using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public class HttpCopyAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "COPY" };

        public HttpCopyAttribute() : base(_supportedMethods)
        {
        }

        public HttpCopyAttribute(string template) : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
