using System;
using System.IO;
using System.Xml;

namespace AmalgaDrive.Drive.WebDav
{
    public class WebDavResource : IRemoteResource
    {
        public WebDavResource(string href, XmlElement propstat)
        {
            if (href == null)
                throw new ArgumentNullException(nameof(href));

            HRef = href;
        }

        public string HRef { get; }
        public string DisplayName { get; set; }
        public string ContentType { get; set; }
        public string ContentLength { get; set; }
        public string ETag { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public FileAttributes Attributes { get; set; }
    }
}
