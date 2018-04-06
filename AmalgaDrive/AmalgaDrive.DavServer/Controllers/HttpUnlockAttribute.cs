using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public class HttpUnlockAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "UNLOCK" };

        public HttpUnlockAttribute() : base(_supportedMethods)
        {
        }

        public HttpUnlockAttribute(string template) : base(_supportedMethods, template)
        {
        }
    }
}
