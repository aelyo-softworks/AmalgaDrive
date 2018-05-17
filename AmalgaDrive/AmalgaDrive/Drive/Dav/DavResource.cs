using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using ShellBoost.Core;

namespace AmalgaDrive.Drive.Dav
{
    public class DavResource : IRemoteResource
    {
        public DavResource(RemoteOperationContext context, string href, XmlElement propstat)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (href == null)
                throw new ArgumentNullException(nameof(href));

            HRef = new Uri(href);
            if (propstat != null)
            {
                var prop = propstat["prop", DavFileSystem.DavNamespaceUri];
                if (prop != null)
                {
                    DisplayName = prop["displayname", DavFileSystem.DavNamespaceUri]?.InnerText;
                    if (string.IsNullOrWhiteSpace(DisplayName))
                    {
                        DisplayName = HRef.LocalPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    }

                    ContentType = prop["getcontenttype", DavFileSystem.DavNamespaceUri]?.InnerText;
                    var folder = prop["resourcetype", DavFileSystem.DavNamespaceUri]?["collection", DavFileSystem.DavNamespaceUri]?.LocalName == "collection";
                    ETag = prop["getetag", DavFileSystem.DavNamespaceUri]?.InnerText;
                    var cl = prop["getcontentlength", DavFileSystem.DavNamespaceUri]?.InnerText;
                    if (cl != null && long.TryParse(cl, out var l))
                    {
                        ContentLength = l;
                    }

                    var dt = prop["creationdate", DavFileSystem.DavNamespaceUri]?.InnerText;
                    if (DateTime.TryParse(dt, out DateTime date))
                    {
                        CreationTimeUtc = date.ToUniversalTime();
                    }

                    dt = prop["getlastmodified", DavFileSystem.DavNamespaceUri]?.InnerText;
                    if (DateTime.TryParse(dt, out date))
                    {
                        LastWriteTimeUtc = date.ToUniversalTime();
                    }

                    var fa = prop["Win32FileAttributes"]?.InnerText;
                    if (fa != null && int.TryParse(fa, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int atts))
                    {
                        Attributes = ((FileAttributes)atts) & context.Synchronizer.SynchronizableAttributes;
                    }

                    if (folder)
                    {
                        Attributes |= FileAttributes.Directory;
                    }
                }
            }
        }

        public Uri HRef { get; }
        public string DisplayName { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public string ETag { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public FileAttributes Attributes { get; set; }

        public override string ToString() => DisplayName;
    }
}
