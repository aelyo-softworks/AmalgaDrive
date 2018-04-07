using System;
using System.IO;

namespace AmalgaDrive.DavServer.FileSystem.Local
{
    public class LocalFileInfo : IFileInfo
    {
        public LocalFileInfo(FileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Info = info;
        }

        public FileInfo Info { get; }
        public long Length => Info.Length;
        public string Name => Info.Name;
        public string ContentType => null;

        public DateTime LastAccessTimeUtc { get => Info.LastAccessTimeUtc; set => Info.LastAccessTimeUtc = value; }
        public DateTime CreationTimeUtc { get => Info.CreationTimeUtc; set => Info.CreationTimeUtc = value; }
        public DateTime LastWriteTimeUtc { get => Info.LastWriteTimeUtc; set => Info.LastWriteTimeUtc = value; }
        public FileAttributes Attributes { get => Info.Attributes; set => Info.Attributes = value; }

        public void Delete() => Info.Delete();
        public Stream OpenRead() => Info.OpenRead();
        public Stream OpenWrite() => Info.OpenWrite();
    }
}
