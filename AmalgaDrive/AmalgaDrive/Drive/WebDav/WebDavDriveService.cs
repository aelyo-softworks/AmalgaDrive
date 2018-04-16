using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Windows.Media;
using System.Xml;
using AmalgaDrive.Configuration;
using AmalgaDrive.Utilities;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Drive.WebDav
{
    [DisplayName("WebDav")]
    public class WebDavDriveService : IRemoteDriveService
    {
        public const string DavNamespaceUri = "DAV:";
        public const string DavNamespacePrefix = "D";
        public const string MsNamespaceUri = "urn:schemas-microsoft-com:";
        public const string MsNamespacePrefix = "Z";
        public const int MultiStatusCode = 207;

        public static readonly XmlNamespaceManager NsMgr;

        static WebDavDriveService()
        {
            NsMgr = new XmlNamespaceManager(new NameTable());
            NsMgr.AddNamespace(DavNamespacePrefix, DavNamespaceUri);
            NsMgr.AddNamespace(MsNamespacePrefix, MsNamespaceUri);
        }

        public void Initialize(DriveServiceSettings settings,  IDictionary<string, object> dictionary)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Settings = settings;
        }

        public DriveServiceSettings Settings { get; private set; }
        public ImageSource Icon => StockIcon.GetStockBitmap(StockIconId.MYNETWORK, StockIcon.SHGSI.SHGSI_LARGEICON);

        public IRemoteResource GetResource(string path) => throw new System.NotImplementedException();
        public void CreateFolderResource(string path) => throw new System.NotImplementedException();
        public Stream OpenReadResource(string path) => throw new System.NotImplementedException();
        public Stream OpenWriteResource(string path) => throw new System.NotImplementedException();

        public IEnumerable<IRemoteResource> EnumResources(string parentResourcePath)
        {
            using (var client = new WebClient())
            {
                string url = UrlUtilities.UrlCombine(Settings.BaseUrl, parentResourcePath);
                var xml = client.DownloadString(url);

                var doc = new XmlDocument();
                if (!string.IsNullOrWhiteSpace(xml))
                {
                    doc.LoadXml(xml);
                }

                foreach (var response in doc.SelectNodes(DavNamespacePrefix + ":multistatus/" + DavNamespacePrefix + ":/response", NsMgr).OfType<XmlElement>())
                {
                    var href = response["href", DavNamespaceUri]?.InnerText;
                    if (href == null)
                        continue;

                    var resource = new WebDavResource(href, response["propstat", DavNamespaceUri]);
                    yield return resource;
                }
            }
        }
    }
}
