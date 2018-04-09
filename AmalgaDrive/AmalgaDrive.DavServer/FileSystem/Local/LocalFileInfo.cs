using System;
using System.IO;

namespace AmalgaDrive.DavServer.FileSystem.Local
{
    public class LocalFileInfo : IFileInfo
    {
        private Lazy<LocalDirectoryInfo> _parent;

        public LocalFileInfo(LocalFileSystem system, FileInfo info)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (info == null)
                throw new ArgumentNullException(nameof(info));

            System = system;
            Info = info;
            _parent = new Lazy<LocalDirectoryInfo>(() => System.CreateDirectoryInfo(Info.Directory));
        }

        public LocalFileSystem System { get; }
        public FileInfo Info { get; }
        public virtual IDirectoryInfo Parent { get; }
        public virtual long Length => Info.Length;
        public virtual string Name => Info.Name;
        public override string ToString() => Name;

        public virtual DateTime LastAccessTimeUtc { get => Info.LastAccessTimeUtc; set => Info.LastAccessTimeUtc = value; }
        public virtual DateTime CreationTimeUtc { get => Info.CreationTimeUtc; set => Info.CreationTimeUtc = value; }
        public virtual DateTime LastWriteTimeUtc { get => Info.LastWriteTimeUtc; set => Info.LastWriteTimeUtc = value; }
        public virtual FileAttributes Attributes { get => Info.Attributes; set => Info.Attributes = value; }
        IFileSystem IFileSystemInfo.System => System;

        public virtual void Delete() => Info.Delete();
        public virtual Stream OpenRead() => Info.OpenRead();
        public virtual Stream OpenWrite() => Info.OpenWrite();

        public virtual void MoveTo(string rootRelativePath)
        {
            if (rootRelativePath == null)
                throw new ArgumentNullException(nameof(rootRelativePath));

            var target = Path.Combine(System.RootPath, rootRelativePath);
            if (!System.IsChildPath(target))
                throw new ArgumentException(null, nameof(rootRelativePath));

            Info.MoveTo(target);
        }

        public virtual void CopyTo(string rootRelativePath, bool overwrite)
        {
            if (rootRelativePath == null)
                throw new ArgumentNullException(nameof(rootRelativePath));

            var target = Path.Combine(System.RootPath, rootRelativePath);
            if (!System.IsChildPath(target))
                throw new ArgumentException(null, nameof(rootRelativePath));

            Info.CopyTo(target, overwrite);
        }
    }
}
