using System;
using System.IO;
using ShellBoost.Core;

namespace AmalgaDrive.Folder
{
    /// <summary>
    /// This class represents the ShellBoost folder server. It will serve all remote drives from one unique root virtual folder.
    /// </summary>
    public class OnDemandShellFolderServer : ShellFolderServer
    {
        public OnDemandShellFolderServer(DirectoryInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            Info = info;
        }

        /// <summary>
        /// Gets the root path for all sub remote drives
        /// </summary>
        /// <value>The root path.</value>
        public DirectoryInfo Info { get; }

        /// <summary>
        /// Gets the cached root folder.
        /// </summary>
        /// <value>The cached root folder.</value>
        public OnDemandRootFolder RootFolder { get; private set; }

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <param name="idl">The root folder PIDL.</param>
        /// <returns>An instance of the RootShellFolder type.</returns>
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
