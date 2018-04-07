using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmalgaDrive.DavServer.FileSystem.Local
{
    public class LocalFileSystem : IFileSystem
    {
        private static readonly HashSet<string> ReservedFileNames = new HashSet<string>(new string[]
            {
                "con", "prn", "aux", "nul",
                "com0", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
                "lpt0", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
            }, StringComparer.OrdinalIgnoreCase);

        public string RootPath { get; private set; }

        public void Initialize(IDictionary<string, string> properties)
        {
            var path = properties.GetNullifiedValue(nameof(RootPath));
            if (path == null)
                throw new DavServerException("0002: Configuration is missing parameter '" + nameof(RootPath) + "'.");

            if (!Path.IsPathRooted(path))
                throw new DavServerException("0004: Parameter '" + nameof(RootPath) + "' must be rooted.");

            // make sure we use long file names
            if (!path.StartsWith(@"\\?\"))
            {
                path = @"\\?\" + path;
            }
            RootPath = path;
        }

        public bool TryGetItem(string fullPath, out IFileSystemInfo info)
        {
            info = null;
            var segments = fullPath?.Split(Path.PathSeparator.ToString(), StringSplitOptions.RemoveEmptyEntries);
            if (segments == null || segments.Length == 0)
            {
                var di = new DirectoryInfo(RootPath);
                if (di.Exists)
                {
                    info = new LocalDirectoryInfo(di);
                    return true;
                }
                return false;
            }

            if (segments.Any(s => s.Contains("..") || ReservedFileNames.Contains(s)))
                return false;

            string path = Path.Combine(RootPath, Path.Combine(segments));
            if (Extensions.FileExists(path))
            {
                info = new LocalFileInfo(new FileInfo(path));
                return true;
            }

            if (Extensions.DirectoryExists(path))
            {
                info = new LocalDirectoryInfo(new DirectoryInfo(path));
                return true;
            }

            return false;
        }
    }
}
