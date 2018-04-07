using System;
using System.Collections.Generic;
using System.Linq;
using AmalgaDrive.DavServer.Controllers;
using AmalgaDrive.DavServer.FileSystem;
using AmalgaDrive.DavServer.FileSystem.Local;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmalgaDrive.DavServer
{
    public static class DavServerExtensions
    {
        public const string DavServerConfigPath = "DavServer:FileSystem:";
        public const string DavNamespaceUri = "DAV:";
        public const string DavNamespacePrefix = "D";
        public const string MsNamespaceUri = "urn:schemas-microsoft-com:";
        public const string MsNamespacePrefix = "Z";
        public const int MultiStatusCode = 207;
        public const string DesktopIni = "desktop.ini";

        public static void AddDavServer(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddTransient((sp) =>
            {
                var typeName = configuration[DavServerConfigPath + "TypeName"];
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    typeName = typeof(LocalFileSystem).AssemblyQualifiedName;
                }

                var fs = Activator.CreateInstance(Type.GetType(typeName, true)) as IFileSystem;
                if (fs == null)
                    throw new DavServerException("0003: Type '" + typeName + "' is not an " + nameof(IFileSystem) + ".");

                var dic = configuration.GetSection(DavServerConfigPath + "Properties").GetChildren().ToDictionary(c1 => c1.Key, c2 => c2.Value);
                fs.Initialize(dic);
                return fs;
            });
        }

        public static string GetContentType(this IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (info is IFileInfo fi)
                return fi.ContentType;

            return null;
        }

        public static long GetContentLength(this IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (info is IFileInfo fi)
                return fi.Length;

            return 0;
        }

        public static Uri GetHRef(this IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            return new Uri("http://localhost:61786/dav/" + info.Name);
        }

        public static IEnumerable<IFileSystemInfo> EnumerateFileSysteminfo(this IFileSystem fileSystem, int depth)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            if (!fileSystem.TryGetItem(null, out IFileSystemInfo info))
                yield break;

            foreach (var child in EnumerateFileSysteminfo(info, depth))
            {
                yield return child;
            }
        }

        public static IEnumerable<IFileSystemInfo> EnumerateFileSysteminfo(this IFileSystemInfo info, int depth)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            yield return info;
            if (depth == 0)
                yield break;

            if (info is IDirectoryInfo di)
            {
                foreach (var child in di.EnumerateDirectories())
                {
                    yield return child;
                }

                foreach (var child in di.EnumerateFiles())
                {
                    yield return child;
                }
            }

            // infinity is not supported (no recursion)
        }

        public static bool Translate(this HttpRequest request)
        {
            if (request == null)
                return false;

            string translate = request.Headers["translate"];
            return string.IsNullOrWhiteSpace(translate) || !translate.EqualsIgnoreCase("f");
        }

        public static int GetDepth(this HttpRequest request)
        {
            if (request == null)
                return int.MaxValue;

            string depth = request.Headers["Depth"];
            if (string.IsNullOrWhiteSpace(depth))
                return int.MaxValue;

            if (!int.TryParse(depth, out int value))
                return int.MaxValue;

            if (value == 0 || value == 1)
                return value;

            return int.MaxValue;
        }
    }
}
