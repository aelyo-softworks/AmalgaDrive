using System.Collections.Generic;

namespace AmalgaDrive.DavServer.FileSystem
{
    public interface IFileSystem
    {
        string RootPath { get; }

        void Initialize(IDictionary<string, string> properties);
        bool TryGetItem(string fullPath, out IFileSystemInfo info);
    }
}
