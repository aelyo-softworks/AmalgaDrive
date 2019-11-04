using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AmalgaDrive.DavServer.FileSystem;
using AmalgaDrive.DavServer.FileSystem.Local;
using Microsoft.AspNetCore.Builder;
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

        public static void AddFileSystem(this IServiceCollection services, IConfiguration configuration, Action<DavServerOptions> setupAction = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddSingleton((sp) =>
            {
                var typeName = configuration[DavServerConfigPath + "TypeName"];
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    typeName = typeof(LocalFileSystem).AssemblyQualifiedName;
                }

                if (!(Activator.CreateInstance(Type.GetType(typeName, true)) is IFileSystem fs))
                    throw new DavServerException("0003: Type '" + typeName + "' is not an " + nameof(IFileSystem) + ".");

                fs.Initialize(setupAction, GetProperties(configuration));
                return fs;
            });
        }

        private static IDictionary<string, string> GetProperties(IConfiguration configuration) => configuration.GetSection(DavServerConfigPath + "Properties").GetChildren().ToDictionary(c1 => c1.Key, c2 => c2.Value);

        public static DirectoryBrowserOptions GetFileSystemDirectoryBrowserOptions(this IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // only supported impl is for LocalFileSystem
            return LocalFileSystem.GetDirectoryBrowserOptions(GetProperties(configuration));
        }

        public static string GetContentType(this IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (info is IFileInfo fi)
                return fi.System.Options.GetContentType(fi);

            return string.Empty;
        }

        public static string GetContentLength(this IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (info is IFileInfo fi)
                return fi.Length.ToString();

            return string.Empty;
        }

        public static string GetETag(this IFileSystemInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (info is IFileInfo fi)
                return ComputeHash(fi.LastWriteTimeUtc.Ticks.ToString() + "." + fi.Length + "." + (int)fi.Attributes);

            return ComputeHash(info.LastWriteTimeUtc.Ticks.ToString() + "." + (int)info.Attributes);
        }

        public static string ComputeHash(string text)
        {
            if (text == null)
                return null;

            using (var md5 = MD5.Create())
            {
                return Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(text)));
            }
        }

        public static IEnumerable<IFileSystemInfo> EnumerateFileSystemInfo(this IFileSystem fileSystem, int depth)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            if (!fileSystem.TryGetItem(null, out IFileSystemInfo info))
                yield break;

            foreach (var child in EnumerateFileSystemInfo(info, depth))
            {
                yield return child;
            }
        }

        public static IEnumerable<IFileSystemInfo> EnumerateFileSystemInfo(this IFileSystemInfo info, int depth)
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

            // infinity is not supported (no deep recursion)
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
