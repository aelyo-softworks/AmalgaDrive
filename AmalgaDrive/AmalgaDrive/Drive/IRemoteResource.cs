using System;
using System.IO;

namespace AmalgaDrive.Drive
{
    public interface IRemoteResource
    {
        string DisplayName { get; set; }
        string ContentType { get; set; }
        string ContentLength { get; set; }
        string ETag { get; set; }
        DateTime LastWriteTimeUtc { get; set; }
        DateTime CreationTimeUtc { get; set; }
        FileAttributes Attributes { get; set; }
    }
}
