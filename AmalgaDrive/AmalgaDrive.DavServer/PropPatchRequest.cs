using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using AmalgaDrive.DavServer.FileSystem;

namespace AmalgaDrive.DavServer
{
    public class PropPatchRequest
    {
        public PropPatchRequest(XmlDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            Document = document;
            UpdatedProperties = new List<DavProperty>();
            UnknownProperties = new List<DavProperty>();
            UnauthorizedProperties = new List<DavProperty>();
        }

        public XmlDocument Document { get; }
        public IList<DavProperty> UpdatedProperties { get; }
        public IList<DavProperty> UnknownProperties { get; }
        public IList<DavProperty> UnauthorizedProperties { get; }

        public virtual void Update(IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            DateTime dt;
            foreach (var propNode in Document.SelectNodes(DavServerExtensions.DavNamespacePrefix + ":propertyupdate/" + DavServerExtensions.DavNamespacePrefix + ":set/" + DavServerExtensions.DavNamespacePrefix + ":prop/*", PropFindRequest.NsMgr).OfType<XmlElement>())
            {
                // avoid changes when possible
                try
                {
                    switch (propNode.NamespaceURI)
                    {
                        case DavServerExtensions.DavNamespaceUri:
                            break;

                        case DavServerExtensions.MsNamespaceUri:
                            switch (propNode.LocalName)
                            {
                                case "Win32CreationTime":
                                    if (DavProperty.TryGet(propNode.InnerText, out dt))
                                    {
                                        var dtu = dt.ToUniversalTime();
                                        if (info.CreationTimeUtc != dtu)
                                        {
                                            info.CreationTimeUtc = dtu;
                                        }
                                    }
                                    break;

                                case "Win32LastAccessTime":
                                    if (DavProperty.TryGet(propNode.InnerText, out dt))
                                    {
                                        var dtu = dt.ToUniversalTime();
                                        if (info.LastAccessTimeUtc != dtu)
                                        {
                                            info.LastAccessTimeUtc = dtu;
                                        }
                                    }
                                    break;

                                case "Win32LastModifiedTime":
                                    if (DavProperty.TryGet(propNode.InnerText, out dt))
                                    {
                                        var dtu = dt.ToUniversalTime();
                                        if (info.LastWriteTimeUtc != dtu)
                                        {
                                            info.LastWriteTimeUtc = dtu;
                                        }
                                    }
                                    break;

                                case "Win32FileAttributes":
                                    if (DavProperty.TryGetFromHexadecimal(propNode.InnerText, out int i))
                                    {
                                        const FileAttributes allowed = FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly;
                                        var newAtts = (FileAttributes)i;
                                        newAtts &= allowed;

                                        var existing = info.Attributes & allowed;
                                        if (existing != newAtts)
                                        {
                                            if (info.Attributes.HasFlag(FileAttributes.ReadOnly))
                                            {
                                                try
                                                {
                                                    info.Attributes &= ~FileAttributes.ReadOnly;
                                                    info.Attributes = newAtts;
                                                }
                                                finally
                                                {
                                                    if (newAtts.HasFlag(FileAttributes.ReadOnly))
                                                    {
                                                        info.Attributes |= FileAttributes.ReadOnly;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                info.Attributes = newAtts;
                                            }
                                        }
                                    }

                                    UpdatedProperties.Add(new DavProperty(propNode.LocalName, propNode.NamespaceURI));
                                    break;
                            }
                            break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    UnauthorizedProperties.Add(new DavProperty(propNode.LocalName, propNode.NamespaceURI));
                }
            }
        }
    }
}
