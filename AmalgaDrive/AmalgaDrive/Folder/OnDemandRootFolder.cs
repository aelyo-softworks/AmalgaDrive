using System;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace AmalgaDrive.Folder
{
    /// <summary>
    /// This class represents the Amalgadrive root folder. There's nothing special to it.
    /// However it ads the System.StorageProviderUIStatus to display sync status (like OneDrive for example)
    /// </summary>
    public class OnDemandRootFolder : RootShellFolder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnDemandRootFolder"/> class.
        /// </summary>
        /// <param name="server">The folder server.</param>
        /// <param name="idList">The root folder PIDL.</param>
        /// <param name="info">The root folder directory.</param>
        /// <exception cref="ArgumentNullException">
        /// server is null
        /// or
        /// info is null.
        /// </exception>
        public OnDemandRootFolder(OnDemandShellFolderServer server, ShellItemIdList idList, DirectoryInfo info)
            : base(idList, info)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Server = server;

            // since this is a FileSystem oriented NSE, we let base properties pass through
            ReadPropertiesFromShell = true;

            /// We just add the sync status column.
            AddColumn(Props.System.StorageProviderUIStatus, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
        }

        /// <summary>
        /// Gets a property key value.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the value was read, <c>false</c> otherwise.</returns>
        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            // this property is asked by the Shell to display the sync status icon
            if (key == Props.System.StorageProviderUIStatus)
            {
                value = GetSyncState(this);
                return true;
            }
            return base.TryGetPropertyValue(key, out value);
        }

        private static MemoryPropertyStore GetSyncState(ShellItem item)
        {
            var ms = new MemoryPropertyStore();
            ms.SetValue(Props.System.PropList.StatusIcons, "prop:" + Props.System.StorageProviderState.CanonicalName);
            ms.SetValue(Props.System.PropList.StatusIconsDisplayFlag, (uint)2);

            // read the sync state from the shell
            // it works because we have set ReadPropertiesFromShell = true as a passthrough
            var state = item.GetPropertyValue(Props.System.StorageProviderState, 0);
            ms.SetValue(Props.System.StorageProviderState, state);

            return ms;
        }

        /// <summary>
        /// Gets the folder server.
        /// </summary>
        /// <value>The folder server.</value>
        public OnDemandShellFolderServer Server { get; }

        /// <summary>
        /// Creates a file system ShellItem instance for a folder.
        /// We override that because we want to customize every sub folder beneath root.
        /// </summary>
        /// <param name="info">The folder directory.</param>
        /// <returns>An instance of the ShellItem type.</returns>
        protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new OnDemandFolder(this, info);
        protected override ShellItem CreateFileSystemItem(FileInfo info) => new OnDemandItem(this, info);
        protected override ShellItem CreateFileSystemFolder(ShellItemId fileSystemId, string fileSystemPath) => new OnDemandFolder(this, fileSystemId, fileSystemPath);
        protected override ShellItem CreateFileSystemItem(ShellItemId fileSystemId, string fileSystemPath) => new OnDemandItem(this, fileSystemId, fileSystemPath);

        /// <summary>
        /// This represents any child folder beneath root.
        /// </summary>
        private class OnDemandFolder : ShellFolder
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnDemandFolder"/> class.
            /// We just add the same sync status column.
            /// </summary>
            /// <param name="parent">The parent.</param>
            /// <param name="info">The information.</param>
            public OnDemandFolder(ShellFolder parent, DirectoryInfo info) : base(parent, info)
            {
                // since this is a FileSystem oriented NSE, we let base properties pass through
                ReadPropertiesFromShell = true;

                /// We just add the sync status column.
                AddColumn(Props.System.StorageProviderUIStatus, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="OnDemandFolder"/> class.
            /// We just add the same sync status column.
            /// </summary>
            /// <param name="parent">The parent.</param>
            /// <param name="info">The information.</param>
            public OnDemandFolder(ShellFolder parent, ShellItemId fileSystemId, string fileSystemPath) : base(parent, fileSystemId, fileSystemPath)
            {
                // since this is a FileSystem oriented NSE, we let base properties pass through
                ReadPropertiesFromShell = true;

                /// We just add the sync status column.
                AddColumn(Props.System.StorageProviderUIStatus, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            }

            protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new OnDemandFolder(this, info);
            protected override ShellItem CreateFileSystemItem(FileInfo info) => new OnDemandItem(this, info);
            protected override ShellItem CreateFileSystemFolder(ShellItemId fileSystemId, string fileSystemPath) => new OnDemandFolder(this, fileSystemId, fileSystemPath);
            protected override ShellItem CreateFileSystemItem(ShellItemId fileSystemId, string fileSystemPath) => new OnDemandItem(this, fileSystemId, fileSystemPath);

            /// <summary>
            /// Gets a property key value.
            /// </summary>
            /// <param name="key">The property key.</param>
            /// <param name="value">The value.</param>
            /// <returns><c>true</c> if the value was read, <c>false</c> otherwise.</returns>
            public override bool TryGetPropertyValue(PropertyKey key, out object value)
            {
                // this properties is asked by the Shell to display the sync status icon
                if (key == Props.System.StorageProviderUIStatus)
                {
                    value = GetSyncState(this);
                    return true;
                }
                return base.TryGetPropertyValue(key, out value);
            }
        }

        /// <summary>
        /// This represents any child item in a folder.
        /// </summary>
        class OnDemandItem : ShellItem
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnDemandItem"/> class.
            /// </summary>
            /// <param name="parent">The parent folder.</param>
            /// <param name="info">The file.</param>
            public OnDemandItem(ShellFolder parent, FileInfo info) : base(parent, info)
            {
                // since this is a FileSystem oriented NSE, we let base properties pass through
                ReadPropertiesFromShell = true;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="OnDemandItem"/> class.
            /// </summary>
            /// <param name="parent">The parent folder.</param>
            /// <param name="info">The file.</param>
            public OnDemandItem(ShellFolder parent, ShellItemId fileSystemId, string fileSystemPath) : base(parent, fileSystemId, fileSystemPath)
            {
                // since this is a FileSystem oriented NSE, we let base properties pass through
                ReadPropertiesFromShell = true;
            }

            /// <summary>
            /// Gets a property key value.
            /// </summary>
            /// <param name="key">The property key.</param>
            /// <param name="value">The value.</param>
            /// <returns><c>true</c> if the value was read, <c>false</c> otherwise.</returns>
            public override bool TryGetPropertyValue(PropertyKey key, out object value)
            {
                // this properties is asked by the Shell to display the sync status icon
                if (key == Props.System.StorageProviderUIStatus)
                {
                    value = GetSyncState(this);
                    return true;
                }
                return base.TryGetPropertyValue(key, out value);
            }
        }
    }
}
