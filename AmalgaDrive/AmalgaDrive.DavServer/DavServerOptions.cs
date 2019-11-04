using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AmalgaDrive.DavServer.FileSystem;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace AmalgaDrive.DavServer
{
    public class DavServerOptions
    {
        private readonly Lazy<IContentTypeProvider> _ctProvider = new Lazy<IContentTypeProvider>(() => new FileExtensionContentTypeProvider(), true);

        public DavServerOptions()
        {
            // TODO: this must match what's used on the client side. for example if the client uses "http://localhost:61786/dav" then BaseUrl must be equal to "dav"
            // must match AmalgaDrive.DavServerSite's default.html (a/href) and appsettings.json (DirectoryBrowserRequestPath)
            BaseUrl = "dav";
            //BaseUrl = "";
            RootName = "dav";
            ServeHRefsWithoutHost = true;
            var list  = new List<string>();

            // These are directories or files we don't want to serve.
            // We must be consistent with the host/reverse proxy (for example IIS/IISExpress) to not send these segments in DAV results.
            // Otherwise when queried for, the client will get a 404 error back and we can't do nothing against it
            // the list is copied from a standard IIS's <hiddenSegments> section
            // https://docs.microsoft.com/en-us/iis/configuration/system.webserver/security/requestfiltering/hiddensegments/
            list.Add("web.config");
            list.Add("bin");
            list.Add("App_code");
            list.Add("App_GlobalResources");
            list.Add("App_LocalResources");
            list.Add("App_WebReferences");
            list.Add("App_Data");
            list.Add("App_Browsers");

            HiddenSegments = list.ToArray();
        }

        public virtual string[] HiddenSegments { get; }
        public virtual bool ServeHidden { get; set; }
        public virtual string BaseUrl { get; set; }
        public virtual string PublicHost { get; set; }
        public virtual string RootName { get; set; }
        public virtual bool ServeHRefsWithoutHost { get; set; }
        public virtual IContentTypeProvider ContentTypeProvider { get; set; }

        public virtual bool IsHiddenSegment(string segment)
        {
            if (segment == null)
                return false;

            return HiddenSegments.Any(s => s.EqualsIgnoreCase(segment));
        }

        public virtual string GetContentType(IFileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var ct = ContentTypeProvider;
            if (ct == null)
            {
                ct = _ctProvider.Value;
            }

            string ext = Path.GetExtension(info.Name);
            if (!string.IsNullOrWhiteSpace(ext) && ct.TryGetContentType(ext, out string contentType))
                return contentType;

            return "application/octet-stream";
        }

        public virtual bool TryGetRelativePath(HttpRequest request, string url, out string relativePath)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (url == null)
                throw new ArgumentNullException(nameof(url));

            // relative url ?
            if (url.StartsWith("/"))
            {
                url = GetPublicBaseUrl(request) + url;
            }

            relativePath = null;
            string publicBaseUrl = GetPublicBaseUrl(request);
            if (!string.IsNullOrWhiteSpace(BaseUrl))
            {
                publicBaseUrl += "/" + BaseUrl;
            }

            if (!url.StartsWith(publicBaseUrl, StringComparison.OrdinalIgnoreCase))
                return false;

            relativePath = url.Substring(publicBaseUrl.Length).Replace("/", @"\");
            if (relativePath.StartsWith(@"\"))
            {
                relativePath = relativePath.Substring(1);
            }
            return true;
        }

        public virtual string GetPublicBaseUrl(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(PublicHost))
                return request.Scheme + "://" + request.Host.Value;

            return PublicHost;
        }

        public virtual Uri GetPublicUri(HttpRequest request, IFileSystemInfo info)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (info == null)
                throw new ArgumentNullException(nameof(info));

            string publicBaseUrl = GetPublicBaseUrl(request);
            string relativePath = BaseUrl;

            var rel = info.System.GetRelativePath(info).Replace(@"\", "/");
            if (!string.IsNullOrWhiteSpace(rel))
            {
                relativePath += "/" + rel;
            }

            relativePath = Uri.EscapeUriString(relativePath);

            if (info is IDirectoryInfo && !relativePath.EndsWith("/"))
            {
                relativePath += "/";
            }

            if (ServeHRefsWithoutHost)
                return new Uri("/" + relativePath, UriKind.Relative);

            return new Uri(new Uri(publicBaseUrl), relativePath);
        }
    }
}
