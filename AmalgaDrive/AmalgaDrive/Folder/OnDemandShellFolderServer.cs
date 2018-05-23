using System;
using System.IO;
using ShellBoost.Core;

namespace AmalgaDrive.Folder
{
    public class OnDemandShellFolderServer : ShellFolderServer
    {
        public OnDemandShellFolderServer(DirectoryInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Info = info;
        }

        public DirectoryInfo Info { get; }
        public OnDemandRootFolder RootFolder { get; private set; }

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (RootFolder == null)
            {
                RootFolder = new OnDemandRootFolder(this, idl, Info);
            }
            return RootFolder;
        }
    }
}
