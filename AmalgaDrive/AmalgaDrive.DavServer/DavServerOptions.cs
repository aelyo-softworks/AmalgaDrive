using System;
using System.IO;
using AmalgaDrive.DavServer.FileSystem;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace AmalgaDrive.DavServer
{
    public class DavServerOptions
    {
        private Lazy<IContentTypeProvider> _ctProvider = new Lazy<IContentTypeProvider>(() => new FileExtensionContentTypeProvider(), true);

        public DavServerOptions()
        {
            BaseUrl = "dav";
            //BaseUrl = "";
            RootName = "dav";
        }

        public virtual bool ServeHidden { get; set; }
        public virtual string BaseUrl { get; set; }
        public virtual string PublicHost { get; set; }
        public virtual string RootName { get; set; }
        public virtual IContentTypeProvider ContentTypeProvider { get; set; }

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
            return new Uri(new Uri(publicBaseUrl), relativePath);
        }
    }
}
