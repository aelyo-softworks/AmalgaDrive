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
        public LocalDriveService(IDriveService service, string rootDirectoryPath)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (rootDirectoryPath == null)
                throw new ArgumentNullException(nameof(rootDirectoryPath));

            DriveService = service;
            RootDirectoryPath = rootDirectoryPath;
        }

        public IDriveService DriveService { get; }
        public string RootDirectoryPath { get; }

        public virtual IEnumerable<FileSystemInfo> EnumerateFileSystemItems(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
            }
            yield return null;
        }
    }
}
