using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public class HttpPropPatchAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "PROPPATCH" };

        public HttpPropPatchAttribute() : base(_supportedMethods)
        {
        }

        public HttpPropPatchAttribute(string template) : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
