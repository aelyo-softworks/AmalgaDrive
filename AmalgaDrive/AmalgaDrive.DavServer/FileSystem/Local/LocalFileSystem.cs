using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace AmalgaDrive.DavServer.FileSystem.Local
{
    public class LocalFileSystem : IFileSystem
    {
        public const string LongFileNamePrefix = @"\\?\";

        private static readonly HashSet<string> _reservedFileNames = new HashSet<string>(new string[]
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
        public virtual DavServerOptions Options { get; }
        public override string ToString() => RootPath;

        public virtual LocalFileInfo NewFileInfo(FileInfo info) => new LocalFileInfo(this, info);
        public virtual LocalDirectoryInfo NewDirectoryInfo(DirectoryInfo info) => new LocalDirectoryInfo(this, info);
        public virtual LocalRootDirectoryInfo NewRootDirectoryInfo(DirectoryInfo info) => new LocalRootDirectoryInfo(this, info);

        public virtual void Initialize(Action<DavServerOptions> setupAction, IDictionary<string, string> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var path = properties.GetNullifiedValue(nameof(RootPath));
            if (string.IsNullOrWhiteSpace(path))
                throw new DavServerException("0002: Configuration is missing parameter '" + nameof(RootPath) + "'.");

            if (!Path.IsPathRooted(path))
                throw new DavServerException("0004: Parameter '" + nameof(RootPath) + "' must be rooted.");

            // make sure we use long file names
            RootPath = path;
            if (!RootPath.StartsWith(@"\\?\"))
            {
                RootPath = LongFileNamePrefix + RootPath;
            }

            if (!Extensions.DirectoryExists(RootPath))
                throw new DavServerException("0006: Directory root path '" + path + "' cannot be found, please check the appsettings.json file.");

            setupAction?.Invoke(Options);
        }

        public static DirectoryBrowserOptions GetDirectoryBrowserOptions(IDictionary<string, string> properties)
        {
            if (properties == null)
                return null;

            var path = properties.GetNullifiedValue("DirectoryBrowserRequestPath");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var rootPath = properties.GetNullifiedValue(nameof(RootPath));
            if (string.IsNullOrWhiteSpace(rootPath))
                return null;

            var options = new DirectoryBrowserOptions();
            options.RequestPath = path;
            try
            {
                options.FileProvider = new PhysicalFileProvider(rootPath);
            }
            catch (Exception e)
            {
                throw new DavServerException("0005: There is a configuration error, please check the appsettings.json file.", e);
            }
            return options;
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
                    info = NewRootDirectoryInfo(di);
                    if (info.Attributes.HasFlag(FileAttributes.Hidden) && !Options.ServeHidden)
                        return false;

                    return true;
                }
                return false;
            }

            if (segments.Any(s => s.Contains("..") || _reservedFileNames.Contains(s)))
                return false;

            string path = Path.Combine(RootPath, Path.Combine(segments));
            if (Extensions.FileExists(path))
            {
                info = NewFileInfo(new FileInfo(path));
                if (info.Attributes.HasFlag(FileAttributes.Hidden) && !Options.ServeHidden)
                    return false;

                return true;
            }

            if (Extensions.DirectoryExists(path))
            {
                info = NewDirectoryInfo(new DirectoryInfo(path));
                if (info.Attributes.HasFlag(FileAttributes.Hidden) && !Options.ServeHidden)
                    return false;

                return true;
            }

            return false;
        }
    }
}
