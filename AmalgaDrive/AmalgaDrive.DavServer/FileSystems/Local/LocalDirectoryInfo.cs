using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AmalgaDrive.DavServer.Model;

namespace AmalgaDrive.DavServer.FileSystems.Local
{
    public class LocalDirectoryInfo : IDirectoryInfo
    {
        public LocalDirectoryInfo(DirectoryInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Info = info;
        }

        public DirectoryInfo Info { get; }
        public string Name => Info.Name;
        public DateTime LastAccessTimeUtc { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime CreationTimeUtc { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime LastWriteTimeUtc { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public FileAttributes Attributes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Delete(bool recursive) => throw new NotImplementedException();
        public void Delete() => throw new NotImplementedException();

        public IEnumerable<IDirectoryInfo> EnumerateDirectories() => throw new NotImplementedException();
        public IEnumerable<IFileInfo> EnumerateFiles() => throw new NotImplementedException();
    }
}
