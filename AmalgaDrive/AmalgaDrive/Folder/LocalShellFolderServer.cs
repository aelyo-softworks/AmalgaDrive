using System;
using AmalgaDrive.Drive;
using ShellBoost.Core;

namespace AmalgaDrive.Folder
{
    public class LocalShellFolderServer : ShellFolderServer
    {
        private RootFolder _root;

        public LocalShellFolderServer(LocalDriveService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            DriveService = service;
        }

        public LocalDriveService DriveService { get; }

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new RootFolder(this, idl);
            }
            return _root;
        }
    }
}
