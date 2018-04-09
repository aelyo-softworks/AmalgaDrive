using System;
using System.IO;

namespace AmalgaDrive.DavServer.FileSystem
{
    public interface IFileSystemInfo
    {
        IFileSystem System { get; }
        IDirectoryInfo Parent { get; }
        DateTime LastAccessTimeUtc { get; set; }
        DateTime CreationTimeUtc { get; set; }
        DateTime LastWriteTimeUtc { get; set; }
        string Name { get; }
        FileAttributes Attributes { get; set; }

        void Delete();
        void MoveTo(string path);
    }
}
