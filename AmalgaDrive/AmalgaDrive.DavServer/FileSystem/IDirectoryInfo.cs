using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmalgaDrive.DavServer.FileSystem
{
    public interface IDirectoryInfo : IFileSystemInfo
    {
        IEnumerable<IFileInfo> EnumerateFiles();
        IEnumerable<IDirectoryInfo> EnumerateDirectories();

        void Delete(bool recursive);
    }
}
