using System;
using System.IO;

namespace AmalgaDrive.DavServer.Model
{
    public interface IFileSystemInfo
    {
        DateTime LastAccessTimeUtc { get; set; }
        DateTime CreationTimeUtc { get; set; }
        DateTime LastWriteTimeUtc { get; set; }
        string Name { get; }
        FileAttributes Attributes { get; set; }

        void Delete();
    }
}
