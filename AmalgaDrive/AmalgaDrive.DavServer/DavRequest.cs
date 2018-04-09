using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;

namespace AmalgaDrive.DavServer
{
    public class DavRequest
    {
        public static readonly XmlNamespaceManager NsMgr;

        static DavRequest()
        {
            NsMgr = new XmlNamespaceManager(new NameTable());
            NsMgr.AddNamespace(DavServerExtensions.DavNamespacePrefix, DavServerExtensions.DavNamespaceUri);
            NsMgr.AddNamespace(DavServerExtensions.MsNamespacePrefix, DavServerExtensions.MsNamespaceUri);
        }

        public DavRequest(XmlDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            Document = document;
            KnownProperties = new List<DavProperty>();
            UnknownProperties = new List<DavProperty>();
            foreach (var propNode in Document.SelectNodes(DavServerExtensions.DavNamespacePrefix + ":propfind/" + DavServerExtensions.DavNamespacePrefix + ":prop/*", NsMgr).OfType<XmlElement>())
            {
                var kp = DavProperty.AllProperties.FirstOrDefault(p => p.Name == propNode.Name && p.NamespaceUri == propNode.NamespaceURI);
                if (kp != null)
                {
                    KnownProperties.Add(kp);
                }
                else
                {
                    var prop = new DavProperty(propNode.LocalName, propNode.NamespaceURI, null);
                    UnknownProperties.Add(prop);
                }
            }

            AllProperties = document.SelectSingleNode(DavServerExtensions.DavNamespacePrefix + ":propfind/" + DavServerExtensions.DavNamespacePrefix + ":allprop", NsMgr) != null;
            AllPropertiesNames = document.SelectSingleNode(DavServerExtensions.DavNamespacePrefix + ":propfind/" + DavServerExtensions.DavNamespacePrefix + ":propname", NsMgr) != null;

            if (KnownProperties.Count == 0)
            {
                AllProperties = true;
                KnownProperties = new List<DavProperty>(DavProperty.AllProperties);
            }
        }

        public XmlDocument Document { get; }
        public IList<DavProperty> KnownProperties { get; }
        public IList<DavProperty> UnknownProperties { get; }
        public bool AllProperties { get; }
        public bool AllPropertiesNames { get; }

        public async Task WriteProperties(XmlWriter writer, IFileSystemInfo info)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (KnownProperties.Count > 0)
            {
                var values = new List<Tuple<DavProperty, object>>();
                foreach (var prop in KnownProperties)
                {
                    var value = prop.GetValueFunc(info);
                    if (value == null)
                        continue;

                    values.Add(new Tuple<DavProperty, object>(prop, value));
                }

                if (values.Count > 0)
                {
                    await writer.WriteStartElementAsync(null, "propstat", DavServerExtensions.DavNamespaceUri);
                    await writer.WriteStartElementAsync(null, "prop", DavServerExtensions.DavNamespaceUri);
                    foreach (var tuple in values)
                    {
                        var prop = tuple.Item1;
                        var value = tuple.Item2;
                        await writer.WriteStartElementAsync(null, prop.Name, prop.NamespaceUri);
                        if (!AllPropertiesNames)
                        {
                            if (prop.WriteValueFunc != null)
                            {
                                await prop.WriteValueFunc(info, writer);
                            }
                            else
                            {
                                if (!(value is string svalue))
                                {
                                    if (value is DateTime dt)
                                    {
                                        svalue = dt.ToString("R");
                                    }
                                    else if (value is bool b)
                                    {
                                        svalue = b ? "1" : "0";
                                    }
                                    else
                                    {
                                        svalue = string.Format(CultureInfo.InvariantCulture, "{0}", value);
                                    }
                                }
                                await writer.WriteStringAsync(svalue);
                            }
                        }
                        await writer.WriteEndElementAsync();
                    }
                    await writer.WriteEndElementAsync();
                    await writer.WriteElementStringAsync(null, "status", DavServerExtensions.DavNamespaceUri, "HTTP/1.1 200 OK");
                    await writer.WriteEndElementAsync();
                }
            }

            if (UnknownProperties.Count > 0)
            {
                await writer.WriteStartElementAsync(null, "propstat", DavServerExtensions.DavNamespaceUri);
                await writer.WriteStartElementAsync(null, "prop", DavServerExtensions.DavNamespaceUri);
                foreach (var prop in UnknownProperties)
                {
                    await writer.WriteStartElementAsync(null, prop.Name, prop.NamespaceUri);
                    await writer.WriteEndElementAsync();
                }
                await writer.WriteEndElementAsync();
                await writer.WriteElementStringAsync(null, "status", DavServerExtensions.DavNamespaceUri, "HTTP/1.1 404 Not Found");
                await writer.WriteEndElementAsync();
            }
        }
    }
}
