using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public class HttpMoveAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "MOVE" };

        public HttpMoveAttribute() : base(_supportedMethods)
        {
        }

        public HttpMoveAttribute(string template) : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
