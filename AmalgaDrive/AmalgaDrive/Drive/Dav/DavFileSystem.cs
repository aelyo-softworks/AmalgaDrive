using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Windows.Media;
using System.Xml;
using AmalgaDrive.Configuration;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Drive.Dav
{
    [DisplayName("WebDav")]
    public class DavFileSystem : IDriveService
    {
        public const string DavNamespaceUri = "DAV:";
        public const string DavNamespacePrefix = "D";
        public const string MsNamespaceUri = "urn:schemas-microsoft-com:";
        public const string MsNamespacePrefix = "Z";
        public const int MultiStatusCode = 207;

        public static readonly XmlNamespaceManager NsMgr;

        static DavFileSystem()
        {
            NsMgr = new XmlNamespaceManager(new NameTable());
            NsMgr.AddNamespace(DavNamespacePrefix, DavNamespaceUri);
            NsMgr.AddNamespace(MsNamespacePrefix, MsNamespaceUri);
        }

        public DriveServiceSettings Settings { get; private set; }

        public virtual void Initialize(DriveServiceSettings settings, IDictionary<string, object> dictionary)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Settings = settings;
        }

        public ImageSource Icon => StockIcon.GetStockBitmap(StockIconId.MYNETWORK, StockIcon.SHGSI.SHGSI_LARGEICON);

        protected virtual WebClient CreateWebClient() => new WebClient();

        protected virtual void SetupWebClient(WebClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (!string.IsNullOrWhiteSpace(Settings.Login))
            {
                client.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
                client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(Settings.Login + ":" + Settings.Password.ToInsecureString())));
            }
        }

        public virtual bool TryGetPropertyValue(string name, out object value)
        {
            value = null;
            return false;
        }

        private static void AddProperty(XmlElement prop, string localName, string ns, object value)
        {
            var set = prop.OwnerDocument.CreateElement(null, localName, ns);
            prop.AppendChild(set);

            if (value is DateTime dt)
            {
                set.InnerText = dt.ToUniversalTime().ToString("R");
                return;
            }

            set.InnerText = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public virtual void UpdateResource(RemoteOperationContext context, string path, IRemoteResource resource, IReadOnlyDictionary<string, object> properties)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            var uri = GetUri(path);

            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                var inputDoc = new XmlDocument();
                var pud = inputDoc.CreateElement(null, "propertyupdate", DavNamespaceUri);
                inputDoc.AppendChild(pud);
                var set = inputDoc.CreateElement(null, "set", DavNamespaceUri);
                pud.AppendChild(set);
                var prop = inputDoc.CreateElement(null, "prop", DavNamespaceUri);
                set.AppendChild(prop);

                AddProperty(prop, "Win32FileAttributes", MsNamespaceUri, ((int)resource.Attributes).ToHex());
                AddProperty(prop, "Win32CreationTime", MsNamespaceUri, resource.CreationTimeUtc);
                AddProperty(prop, "Win32LastModifiedTime", MsNamespaceUri, resource.LastWriteTimeUtc);

                string xml;
                try
                {
                    xml = client.UploadString(uri, "PROPPATCH", string.Empty);
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on PROPPATCH " + uri + ": " + e.Message);
                    // continue
                    xml = null;
                }

                var doc = new XmlDocument();
                if (!string.IsNullOrWhiteSpace(xml))
                {
                    try
                    {
                        doc.LoadXml(xml);
                    }
                    catch (Exception e)
                    {
                        context.AddError(e);
                        context.Log(TraceLevel.Error, "Error on LoadXml " + uri + ": " + e.Message);
                        // continue
                    }
                }
            }
        }

        public virtual void UploadResource(RemoteOperationContext context, string path, Stream input)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var uri = GetUri(path);

            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                try
                {
                    using (var stream = client.OpenWrite(uri, "PUT"))
                    {
                        input.CopyTo(stream);
                    }
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on PUT " + uri + ": " + e.Message);
                    // continue
                }
            }
        }

        public virtual void DownloadResource(RemoteOperationContext context, string path, long offset, long count, Stream output)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var uri = GetUri(path);

            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                // https://referencesource.microsoft.com/#mscorlib/system/io/stream.cs
                // we pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
                var buffer = new byte[81920];

                try
                {
                    using (var stream = client.OpenRead(uri))
                    {
                        // note: we don't use range as not all servers support this, we that would be an optimization
                        while (offset > 0)
                        {
                            int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, offset));
                            offset -= read;
                        }

                        while (count > 0)
                        {
                            int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
                            count -= read;
                            output.Write(buffer, 0, read);
                        }
                    }
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on GET " + uri + ": " + e.Message);
                    // continue
                }
            }
        }

        public virtual void CreateDirectory(RemoteOperationContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var uri = GetUri(path);

            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                try
                {
                    client.UploadString(uri, "MKCOL", string.Empty);
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on MKCOL " + uri + ": " + e.Message);
                }
            }
        }

        public virtual void DeleteResource(RemoteOperationContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var uri = GetUri(path);

            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                try
                {
                    client.UploadString(uri, "DELETE", string.Empty);
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on DELETE " + uri + ": " + e.Message);
                }
            }
        }

        public virtual IRemoteResource GetResource(RemoteOperationContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return EnumResources(context, path).FirstOrDefault();
        }

        public Uri GetUri(string parentPath)
        {
            if (parentPath == null)
                throw new ArgumentNullException(nameof(parentPath));

            // note this code handles file names that contains url special chars, like '#'
            var builder = new UriBuilder(Settings.BaseUri.Scheme, Settings.BaseUri.Host, Settings.BaseUri.Port, Settings.BaseUri.AbsolutePath);
            if (parentPath != null)
            {
                builder.Path = IOUtilities.UrlCombine(builder.Path, parentPath);
            }
            return builder.Uri;
        }

        public virtual IEnumerable<IRemoteResource> EnumResources(RemoteOperationContext context, string parentPath)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var uri = GetUri(parentPath);

            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);
                client.Headers["depth"] = "1";

                string xml;
                try
                {
                    xml = client.UploadString(uri, "PROPFIND", string.Empty);
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on PROPFIND " + uri + ": " + e.Message);
                    // continue
                    xml = null;
                }

                var doc = new XmlDocument();
                if (!string.IsNullOrWhiteSpace(xml))
                {
                    try
                    {
                        doc.LoadXml(xml);
                    }
                    catch (Exception e)
                    {
                        context.AddError(e);
                        context.Log(TraceLevel.Error, "Error on LoadXml " + uri + ": " + e.Message);
                        // continue
                    }
                }

                foreach (var response in doc.SelectNodes(DavNamespacePrefix + ":multistatus/" + DavNamespacePrefix + ":response", NsMgr).OfType<XmlElement>())
                {
                    var href = response["href", DavNamespaceUri]?.InnerText;
                    if (href == null)
                        continue;

                    // handle relative urls
                    if (href.StartsWith("/"))
                    {
                        href = Settings.BaseUri.Scheme + "://" + Settings.BaseUri.Authority + href;
                    }
                    else if (!href.StartsWith("http:") && !href.StartsWith("https:"))
                    {
                        href = IOUtilities.UrlCombine(Settings.BaseUri.ToString(), href);
                    }

                    // skip root dir
                    string uris = uri.ToString();
                    var hrefUri = new Uri(href).ToString(); // handle escaping
                    if (hrefUri == uris || (!uris.EndsWith("/") && hrefUri == uris + "/"))
                        continue;

                    var resource = new DavResource(context, href, response["propstat", DavNamespaceUri]);
                    yield return resource;
                }
            }
        }
    }
}
