using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Media;
using System.Xml;
using AmalgaDrive.Model;
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

        public DriveService DriveService { get; private set; }

        public virtual void Initialize(DriveService driveService, IDictionary<string, object> dictionary)
        {
            if (driveService == null)
                throw new ArgumentNullException(nameof(driveService));

            DriveService = driveService;
        }

        public ImageSource Icon => StockIcon.GetStockBitmap(StockIconId.MYNETWORK, StockIcon.SHGSI.SHGSI_LARGEICON);

        private WebClient2 CreateWebClient() => new WebClient2();
        private class WebClient2 : WebClient
        {
            // this is mostly used for HEAD witch doesn't support null nor empty body
            public string Method { get; set; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var webRequest = base.GetWebRequest(address);
                if (!string.IsNullOrEmpty(Method))
                {
                    webRequest.Method = Method;
                }
                return webRequest;
            }
        }

        protected virtual void SetupWebClient(WebClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (!string.IsNullOrWhiteSpace(DriveService.Login))
            {
                client.Credentials = new NetworkCredential(DriveService.Login, DriveService.Password);
                client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(DriveService.Login + ":" + DriveService.Password.ToInsecureString())));
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

            // properties can be null

            // rename is when the resource name doesn't match the path
            bool rename = resource.DisplayName != null && !resource.DisplayName.EqualsIgnoreCase(Path.GetFileName(path));
            var uri = GetUri(path);
            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                string xml;
                if (rename)
                {
                    client.Headers["Overwrite"] = "t";
                    var newPath = Path.Combine(Path.GetDirectoryName(path), resource.DisplayName);
                    var newUri = GetUri(newPath);
                    client.Headers["Destination"] = newUri.AbsolutePath;

                    try
                    {
                        xml = client.UploadString(uri, "MOVE", string.Empty);
                    }
                    catch (Exception e)
                    {
                        context.AddError(e);
                        context.Log(TraceLevel.Error, "Error on MOVE " + uri + ": " + e.Message);
                        throw;
                    }
                }
                else
                {
                    var inputDoc = new XmlDocument();
                    var pud = inputDoc.CreateElement(null, "propertyupdate", DavNamespaceUri);
                    inputDoc.AppendChild(pud);
                    var set = inputDoc.CreateElement(null, "set", DavNamespaceUri);
                    pud.AppendChild(set);
                    var prop = inputDoc.CreateElement(null, "prop", DavNamespaceUri);
                    set.AppendChild(prop);

                    AddProperty(prop, "Win32FileAttributes", MsNamespaceUri, ((int)resource.Attributes).ToHex());

                    if (resource.CreationTimeUtc != DateTime.MinValue)
                    {
                        AddProperty(prop, "Win32CreationTime", MsNamespaceUri, resource.CreationTimeUtc);
                    }

                    if (resource.LastWriteTimeUtc != DateTime.MinValue)
                    {
                        AddProperty(prop, "Win32LastModifiedTime", MsNamespaceUri, resource.LastWriteTimeUtc);
                    }

                    try
                    {
                        xml = client.UploadString(uri, "PROPPATCH", inputDoc.OuterXml);
                    }
                    catch (Exception e)
                    {
                        context.AddError(e);
                        context.Log(TraceLevel.Error, "Error on PROPPATCH " + uri + ": " + e.Message);
                        throw;
                    }
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
                        throw;
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

            var uri = GetUri(path);
            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                // input can be null for empty files (or files that are locked but still must exist on the server)
                if (input == null)
                {
                    // but if the file already exists, don't overwrite it with an empty body
                    if (ResourceExists(client, uri))
                        return;
                }

                try
                {
                    using (var stream = client.OpenWrite(uri, "PUT"))
                    {
                        input?.CopyTo(stream);
                    }

                    // Note: if you get a 404 error on this line while everything else seems ok and usully works,
                    // it may be due to an upload size limit from the server (limit value is by default around 30M).
                    // Check AmalgaDrive.DavServerSite Program.cs for more information
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on PUT " + uri + ": " + e.Message);
                    throw;
                }
            }
        }

        private static bool ResourceExists(WebClient2 client, Uri uri)
        {
            try
            {
                client.Method = "HEAD"; // HEAD is special, it's not supported by standard WebClient because it must have a null body but the Upload method don't accept it
                client.DownloadString(uri);
                return true;
            }
            catch (Exception e)
            {
                if (!IsNotFound(e))
                    throw;

                return false;
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
                        // note: we don't use range as not all servers support this, but that would be an optimization

                        // skip [offset] bytes
                        while (offset > 0)
                        {
                            int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, offset));
                            offset -= read;
                        }

                        // read & write [count] bytes
                        while (count > 0)
                        {
                            int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
                            count -= read;
                            if (read > 0)
                            {
                                output.Write(buffer, 0, read);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on GET " + uri + ": " + e.Message);
                    throw;
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
                    throw;
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
                    if (!IsNotFound(e)) // already deleted?
                    {
                        context.AddError(e);
                        context.Log(TraceLevel.Error, "Error on DELETE " + uri + ": " + e.Message);
                        throw;
                    }

                    context.Log(TraceLevel.Warning, "Error on DELETE " + uri + ": " + e.Message);
                }
            }
        }

        public virtual IRemoteResource GetResource(RemoteOperationContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return EnumResources(context, path, false).FirstOrDefault();
        }

        public Uri GetUri(string parentPath)
        {
            if (parentPath == null)
                throw new ArgumentNullException(nameof(parentPath));

            // note this code handles file names that contains url special chars, like '#'
            var builder = new UriBuilder(DriveService.BaseUri.Scheme, DriveService.BaseUri.Host, DriveService.BaseUri.Port, DriveService.BaseUri.AbsolutePath);
            if (parentPath != null)
            {
                builder.Path = IOUtilities.UrlCombine(builder.Path, parentPath);
            }
            return builder.Uri;
        }

        public virtual IEnumerable<IRemoteResource> EnumResources(RemoteOperationContext context, string parentPath) => EnumResources(context, parentPath, true);
        private IEnumerable<IRemoteResource> EnumResources(RemoteOperationContext context, string parentPath, bool children)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var uri = GetUri(parentPath);
            using (var client = CreateWebClient())
            {
                if (client == null)
                    throw new InvalidOperationException();

                SetupWebClient(client);

                if (children)
                {
                    client.Headers["depth"] = "1";
                }

                string xml;
                try
                {
                    xml = client.UploadString(uri, "PROPFIND", string.Empty);
                }
                catch (Exception e)
                {
                    // 404?
                    if (!IsNotFound(e))
                    {
                        context.AddError(e);
                        context.Log(TraceLevel.Error, "Error on PROPFIND " + uri + ": " + e.Message);
                        throw;
                    }

                    context.Log(TraceLevel.Warning, "Error on PROPFIND " + uri + ": " + e.Message);
                    xml = null;
                }

                if (string.IsNullOrWhiteSpace(xml))
                    yield break;

                var doc = new XmlDocument();
                try
                {
                    doc.LoadXml(xml);
                }
                catch (Exception e)
                {
                    context.AddError(e);
                    context.Log(TraceLevel.Error, "Error on LoadXml " + uri + ": " + e.Message);
                    throw;
                }

                foreach (var response in doc.SelectNodes(DavNamespacePrefix + ":multistatus/" + DavNamespacePrefix + ":response", NsMgr).OfType<XmlElement>())
                {
                    var href = response["href", DavNamespaceUri]?.InnerText;
                    if (href == null)
                        continue;

                    // handle relative urls
                    if (href.StartsWith("/"))
                    {
                        href = DriveService.BaseUri.Scheme + "://" + DriveService.BaseUri.Authority + href;
                    }
                    else if (!href.StartsWith("http:") && !href.StartsWith("https:"))
                    {
                        href = IOUtilities.UrlCombine(DriveService.BaseUri.ToString(), href);
                    }

                    if (children)
                    {
                        // skip root dir
                        string uris = uri.ToString();
                        var hrefUri = new Uri(href).ToString(); // handle escaping
                        if (hrefUri == uris || (!uris.EndsWith("/") && hrefUri == uris + "/"))
                            continue;
                    }

                    var resource = new DavResource(context, href, response["propstat", DavNamespaceUri]);
                    yield return resource;
                }
            }
        }

        private static bool IsNotFound(Exception e) => e is WebException we && we.Response is HttpWebResponse hw && hw.StatusCode == HttpStatusCode.NotFound;
    }
}
