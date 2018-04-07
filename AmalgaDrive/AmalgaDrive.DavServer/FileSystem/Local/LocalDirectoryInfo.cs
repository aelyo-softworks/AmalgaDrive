using System;
using System.Collections.Generic;
using System.IO;

namespace AmalgaDrive.DavServer.FileSystem.Local
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

        public DateTime LastAccessTimeUtc { get => Info.LastAccessTimeUtc; set => Info.LastAccessTimeUtc = value; }
        public DateTime CreationTimeUtc { get => Info.CreationTimeUtc; set => Info.CreationTimeUtc = value; }
        public DateTime LastWriteTimeUtc { get => Info.LastWriteTimeUtc; set => Info.LastWriteTimeUtc = value; }
        public FileAttributes Attributes { get => Info.Attributes; set => Info.Attributes = value; }

        public void Delete(bool recursive) => Info.Delete(recursive);
        public void Delete() => Info.Delete();

        public IEnumerable<IDirectoryInfo> EnumerateDirectories()
        {
            foreach (var dir in Info.EnumerateDirectories())
            {
                yield return new LocalDirectoryInfo(dir);
            }
        }

        public IEnumerable<IFileInfo> EnumerateFiles()
        {
            foreach (var file in Info.EnumerateFiles())
            {
                yield return new LocalFileInfo(file);
            }
        }
    }
}
