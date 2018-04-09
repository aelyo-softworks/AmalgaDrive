using System;
using System.Collections.Generic;

namespace AmalgaDrive.DavServer.FileSystem
{
    public interface IFileSystem
    {
        string RootPath { get; }
        DavServerOptions Options { get; }

        void Initialize(Action<DavServerOptions> setupAction, IDictionary<string, string> properties);
        bool TryGetItem(string fullPath, out IFileSystemInfo info);
        string GetRelativePath(IFileSystemInfo info);
    }
}
