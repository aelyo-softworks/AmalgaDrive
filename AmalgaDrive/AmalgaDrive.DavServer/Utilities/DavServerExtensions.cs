using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using AmalgaDrive.DavServer.FileSystems.Local;
using AmalgaDrive.DavServer.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmalgaDrive.DavServer.Utilities
{
    public static class DavServerExtensions
    {
        public const string DavServerConfigPath = "DavServer:FileSystem:";

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

        public static void WriteProperties(this IFileSystemInfo info, XmlTextWriter writer)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

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

        public static int GetDepthHeader(this HttpRequest request)
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
