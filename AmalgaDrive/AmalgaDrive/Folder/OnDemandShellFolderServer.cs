using System;
using System.IO;
using ShellBoost.Core;

namespace AmalgaDrive.Folder
{
    public class OnDemandShellFolderServer : ShellFolderServer
    {
        private OnDemandRootFolder _root;

        public OnDemandShellFolderServer(DirectoryInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Info = info;
        }

        public DirectoryInfo Info { get; }

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new OnDemandRootFolder(this, idl, Info);
            }
            return _root;
        }
    }
}
