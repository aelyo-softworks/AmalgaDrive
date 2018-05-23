using System;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace AmalgaDrive.Folder
{
    public class OnDemandRootFolder : RootShellFolder
    {
        public OnDemandRootFolder(OnDemandShellFolderServer server, ShellItemIdList idList, DirectoryInfo info)
            : base(idList, info)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Server = server;
            AddColumn(Props.System.StatusIcons, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
        }

        public OnDemandShellFolderServer Server { get; }

        protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new OnDemandFolder(this, info);
        protected override ShellItem CreateFileSystemItem(FileInfo info) => new OnDemandItem(this, info);

        class OnDemandFolder : ShellFolder
        {
            public OnDemandFolder(ShellFolder parent, DirectoryInfo info) : base(parent, info)
            {
                AddColumn(Props.System.StatusIcons, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            }

            protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new OnDemandFolder(this, info);
            protected override ShellItem CreateFileSystemItem(FileInfo info) => new OnDemandItem(this, info);
        }

        class OnDemandItem : ShellItem
        {
            public OnDemandItem(ShellFolder parent, FileInfo info) : base(parent, info)
            {
                ReadPropertiesFromShell = true;
            }
        }
    }
}
