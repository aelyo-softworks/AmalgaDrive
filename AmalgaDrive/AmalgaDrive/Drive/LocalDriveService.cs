using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmalgaDrive.Drive
{
    public class LocalDriveService
    {
        public const string HiddenFolderName = ".amalgadrive";

        public LocalDriveService(IRemoteDriveService service, string rootDirectoryPath)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (rootDirectoryPath == null)
                throw new ArgumentNullException(nameof(rootDirectoryPath));

            RemoteDriveService = service;
            RootDirectoryPath = rootDirectoryPath;
        }

        public IRemoteDriveService RemoteDriveService { get; }
        public string RootDirectoryPath { get; }

        public virtual void SyncWithRemote(string relativePath)
        {
            RemoteDriveService.EnumResources(relativePath);
        }

        public virtual IEnumerable<FileSystemInfo> EnumerateFileSystemItems(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
            }
            yield return null;
        }
    }
}
