using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AmalgaDrive.DavServer.Controllers
{
    public class HttpMkColAttribute : HttpMethodAttribute
    {
        private static readonly string[] _supportedMethods = new[] { "MKCOL" };

        public HttpMkColAttribute()
            : base(_supportedMethods)
        {
        }

        public HttpMkColAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
        }
    }
}
