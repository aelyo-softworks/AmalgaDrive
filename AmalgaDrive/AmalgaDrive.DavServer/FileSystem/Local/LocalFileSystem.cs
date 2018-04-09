using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmalgaDrive.DavServer.FileSystem.Local
{
    public class LocalFileSystem : IFileSystem
    {
        public const string LongFileNamePrefix = @"\\?\";

        private static readonly HashSet<string> ReservedFileNames = new HashSet<string>(new string[]
            {
                "con", "prn", "aux", "nul",
                "com0", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
                "lpt0", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
            }, StringComparer.OrdinalIgnoreCase);

        public LocalFileSystem()
        {
            Options = new DavServerOptions();
        }

        public string RootPath { get; private set; }
        public DavServerOptions Options { get; }
        public override string ToString() => RootPath;

        public virtual LocalFileInfo CreateFileInfo(FileInfo info) => new LocalFileInfo(this, info);
        public virtual LocalDirectoryInfo CreateDirectoryInfo(DirectoryInfo info) => new LocalDirectoryInfo(this, info);
        public virtual LocalRootDirectoryInfo CreateRootDirectoryInfo(DirectoryInfo info) => new LocalRootDirectoryInfo(this, info);

        public virtual void Initialize(Action<DavServerOptions> setupAction, IDictionary<string, string> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var path = properties.GetNullifiedValue(nameof(RootPath));
            if (path == null)
                throw new DavServerException("0002: Configuration is missing parameter '" + nameof(RootPath) + "'.");

            if (!Path.IsPathRooted(path))
                throw new DavServerException("0004: Parameter '" + nameof(RootPath) + "' must be rooted.");

            // make sure we use long file names
            if (!path.StartsWith(@"\\?\"))
            {
                path = LongFileNamePrefix + path;
            }
            RootPath = path;

            setupAction?.Invoke(Options);
        }

        public virtual bool IsChildPath(string path)
        {
            if (path == null)
                return false;

            return path.EqualsIgnoreCase(RootPath) || path.StartsWith(RootPath + @"\");
        }

        public virtual string GetRelativePath(IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            string path;
            if (info is LocalDirectoryInfo dir)
            {
                path = dir.Info.FullName;
            }
            else
            {
                path = ((LocalFileInfo)info).Info.FullName;
            }

            if (!IsChildPath(path))
                throw new ArgumentException(null, nameof(info));

            string relative = path.Substring(RootPath.Length);
            if (relative.StartsWith(@"\"))
            {
                relative = relative.Substring(1);
            }
            return relative;
        }

        public virtual bool TryGetItem(string fullPath, out IFileSystemInfo info)
        {
            // full path can be null
            info = null;
            var segments = fullPath?.Split(Path.PathSeparator.ToString(), StringSplitOptions.RemoveEmptyEntries);
            if (segments == null || segments.Length == 0)
            {
                var di = new DirectoryInfo(RootPath);
                if (di.Exists)
                {
                    info = CreateRootDirectoryInfo(di);
                    if (info.Attributes.HasFlag(FileAttributes.Hidden) && !Options.ServeHidden)
                        return false;

                    return true;
                }
                return false;
            }

            if (segments.Any(s => s.Contains("..") || ReservedFileNames.Contains(s)))
                return false;

            string path = Path.Combine(RootPath, Path.Combine(segments));
            if (Extensions.FileExists(path))
            {
                info = CreateFileInfo(new FileInfo(path));
                if (info.Attributes.HasFlag(FileAttributes.Hidden) && !Options.ServeHidden)
                    return false;

                return true;
            }

            if (Extensions.DirectoryExists(path))
            {
                info = CreateDirectoryInfo(new DirectoryInfo(path));
                if (info.Attributes.HasFlag(FileAttributes.Hidden) && !Options.ServeHidden)
                    return false;

                return true;
            }

            return false;
        }
    }
}
