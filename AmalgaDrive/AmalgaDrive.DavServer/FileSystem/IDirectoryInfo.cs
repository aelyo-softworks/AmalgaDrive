using System.Collections.Generic;

namespace AmalgaDrive.DavServer.FileSystem
{
    public interface IDirectoryInfo : IFileSystemInfo
    {
        bool IsRoot { get; }

        IEnumerable<IFileInfo> EnumerateFiles();
        IEnumerable<IDirectoryInfo> EnumerateDirectories();
        void Delete(bool recursive);
        void Create(string name);
    }
}
