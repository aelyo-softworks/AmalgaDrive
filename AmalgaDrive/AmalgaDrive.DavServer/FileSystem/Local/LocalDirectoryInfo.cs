using System;
using System.Collections.Generic;
using System.IO;

namespace AmalgaDrive.DavServer.FileSystem.Local
{
    public class LocalDirectoryInfo : IDirectoryInfo
    {
        private readonly Lazy<LocalDirectoryInfo> _parent;

        public LocalDirectoryInfo(LocalFileSystem system, DirectoryInfo info)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (info == null)
                throw new ArgumentNullException(nameof(info));

            System = system;
            Info = info;
            _parent = new Lazy<LocalDirectoryInfo>(() => System.NewDirectoryInfo(Info.Parent));
        }

        public LocalFileSystem System { get; }
        public DirectoryInfo Info { get; }
        public virtual IDirectoryInfo Parent => _parent.Value;
        public virtual string Name => Info.Name;
        public virtual bool IsRoot => false;

        public virtual DateTime LastAccessTimeUtc { get => Info.LastAccessTimeUtc; set => Info.LastAccessTimeUtc = value; }
        public virtual DateTime CreationTimeUtc { get => Info.CreationTimeUtc; set => Info.CreationTimeUtc = value; }
        public virtual DateTime LastWriteTimeUtc { get => Info.LastWriteTimeUtc; set => Info.LastWriteTimeUtc = value; }
        public virtual FileAttributes Attributes { get => Info.Attributes; set => Info.Attributes = value; }
        IFileSystem IFileSystemInfo.System => System;

        public virtual void Delete(bool recursive) => Info.Delete(recursive);
        public virtual void Delete() => Info.Delete(true);
        public override string ToString() => Name;

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

            Extensions.CopyDirectory(Info.FullName, target);
        }

        public virtual void Create(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Info.CreateSubdirectory(name);
        }

        public virtual IEnumerable<IDirectoryInfo> EnumerateDirectories()
        {
            foreach (var dir in Info.EnumerateDirectories())
            {
                if (dir.Attributes.HasFlag(FileAttributes.Hidden) && !System.Options.ServeHidden)
                    continue;

                yield return System.NewDirectoryInfo(dir);
            }
        }

        public virtual IEnumerable<IFileInfo> EnumerateFiles()
        {
            foreach (var file in Info.EnumerateFiles())
            {
                if (file.Attributes.HasFlag(FileAttributes.Hidden) && !System.Options.ServeHidden)
                    continue;

                yield return System.NewFileInfo(file);
            }
        }
    }
}
