using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;

namespace AmalgaDrive.Folder
{
    public class LocalShellFolder : ShellFolder
    {
        public LocalShellFolder(ShellFolder parent, DirectoryInfo info)
            : base(parent, info) // there is a specific overload for DirectoryInfo
        {
            CanCopy = true;
            CanDelete = true;
            CanLink = true;
            CanMove = true;
            CanPaste = true;
            CanRename = true;
            Info = info;
        }

        public DirectoryInfo Info { get; }

        // we export this as internal so the roo folder shares this behavior
        internal static IEnumerable<FileSystemInfo> EnumerateFileSystemItems(DirectoryInfo info, string searchPattern)
        {
            foreach (var child in info.EnumerateFileSystemInfos(searchPattern))
            {
                yield return child;
            }
        }

        protected override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(DirectoryInfo info, SHCONTF options, string searchPattern)
        {
            return EnumerateFileSystemItems(info, searchPattern);
        }

        protected override ShellItem CreateFileSystemFolder(DirectoryInfo info)
        {
            return new LocalShellFolder(this, info);
        }

        protected override ShellItem CreateFileSystemItem(FileInfo info)
        {
            return new LocalShellItem(this, info);
        }

        private List<string> GetPaths(DragDropTargetEventArgs e)
        {
            var list = new List<string>();
            var idls = e.DataObject[ShellDataObjectFormat.CFSTR_SHELLIDLIST]?.ConvertedData as IEnumerable<ShellItemIdList>;
            if (idls != null)
            {
                foreach (var idl in idls)
                {
                    string path;
                    var item = Root.GetItem(idl);
                    if (item != null)
                    {
                        // this comes from ourselves
                        path = item.FileSystemPath;
                    }
                    else
                    {
                        // check it's a file system pidl
                        path = idl.GetFileSystemPath();
                    }

                    if (path != null)
                    {
                        list.Add(path);
                    }
                }
            }
            return list;
        }

        protected override void OnDragDropTarget(DragDropTargetEventArgs e)
        {
            e.HResult = ShellUtilities.S_OK;
            var paths = GetPaths(e);
            if (paths.Count > 0)
            {
                e.Effect = DragDropEffects.All;
            }

            if (e.Type == DragDropTargetEventType.DragDrop)
            {
                // file operation events need an STA thread
                WindowsUtilities.DoModelessAsync(() =>
                {
                    using (var fo = new FileOperation(true))
                    {
                        fo.PostCopyItem += (sender, e2) =>
                        {
                            // we could add some logic here
                        };

                        if (paths.Count == 1)
                        {
                            fo.CopyItem(paths[0], FileSystemPath, null);
                        }
                        else
                        {
                            fo.CopyItems(paths, FileSystemPath);
                        }
                        fo.SetOperationFlags(FOF.FOF_ALLOWUNDO | FOF.FOF_NOCONFIRMMKDIR | FOF.FOF_RENAMEONCOLLISION);
                        fo.PerformOperations();
                    }
                });
            }
        }
    }
}
