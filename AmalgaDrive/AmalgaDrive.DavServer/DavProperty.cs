using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;

namespace AmalgaDrive.DavServer
{
    public class DavProperty
    {
        public static readonly DavProperty[] AllProperties;

        static DavProperty()
        {
            var list = new List<DavProperty>();
            list.Add(new DavProperty("displayname", i => i.Name));
            list.Add(new DavProperty("getcontenttype", i => i.GetContentType()));
            list.Add(new DavProperty("getcontentlength", i => i.GetContentLength()));
            list.Add(new DavProperty("getetag", i => i.GetETag()));
            list.Add(new DavProperty("creationdate", i => i.CreationTimeUtc));
            list.Add(new DavProperty("getlastmodified", i => i.LastWriteTimeUtc));
            list.Add(new DavProperty("Win32FileAttributes", DavServerExtensions.MsNamespaceUri, i => ((int)i.Attributes).ToString("X8")));
            list.Add(new DavProperty("resourcetype", i => i) { WriteValueFunc = async (i, w) =>
                         {
                             if (i is IDirectoryInfo)
                             {
                                 await w.WriteStartElementAsync(null, "collection", DavServerExtensions.DavNamespaceUri);
                                 await w.WriteEndElementAsync();
                             }
                         }});
            AllProperties = list.ToArray();
        }

        public DavProperty(string name, Func<IFileSystemInfo, object> getValueFunc = null)
            : this(name, DavServerExtensions.DavNamespaceUri, getValueFunc)
        {
        }

        public DavProperty(string localName, string namespaceUri, Func<IFileSystemInfo, object> getValueFunc = null)
        {
            if (localName == null)
                throw new ArgumentNullException(nameof(localName));

            LocalName = localName;
            NamespaceUri = namespaceUri;
            GetValueFunc = getValueFunc;
        }

        public string LocalName { get; }
        public string NamespaceUri { get; }
        public virtual Func<IFileSystemInfo, object> GetValueFunc { get; set; }
        public virtual Func<IFileSystemInfo, XmlWriter, Task> WriteValueFunc { get; set; }

        public static bool TryGet(string value, out DateTime dt)
        {
            dt = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt);
        }

        public static bool TryGet(string value, out int i)
        {
            i = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return int.TryParse(value, out i);
        }

        public static bool TryGetFromHexadecimal(string value, out int i)
        {
            i = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out i);
        }

        public override string ToString() => NamespaceUri + ":" + LocalName;
    }
}
