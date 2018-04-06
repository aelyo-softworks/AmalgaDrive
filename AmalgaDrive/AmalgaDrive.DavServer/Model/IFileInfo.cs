using System.IO;

namespace AmalgaDrive.DavServer.Model
{
    public interface IFileInfo : IFileSystemInfo
    {
        long Length { get; }

        Stream OpenRead();
        Stream OpenWrite();
    }
}
