using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AmalgaDrive.Drive
{
    [Serializable]
    public class DriveServiceDescriptor
    {
        private DriveServiceDescriptor(Type type)
        {
            AssemblyQualifiedName = type.AssemblyQualifiedName;
            var dna = type.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.FullName == typeof(DisplayNameAttribute).FullName);
            if (dna != null && !string.IsNullOrWhiteSpace((string)dna.ConstructorArguments[0].Value))
            {
                DisplayName = (string)dna.ConstructorArguments[0].Value;
            }
            else
            {
                DisplayName = type.Name;
            }
        }

        public string AssemblyQualifiedName { get; }
        public string DisplayName { get; }

        public override string ToString() => DisplayName;

        public static IEnumerable<DriveServiceDescriptor> ScanDescriptors()
        {
            var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString("N"));
            var pd = (RemoteObject)domain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location, typeof(RemoteObject).FullName);

            try
            {
                return pd.ScanDescriptors(AppDomain.CurrentDomain.BaseDirectory);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        private class RemoteObject : MarshalByRefObject
        {
            public DriveServiceDescriptor[] ScanDescriptors(string path)
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, args) => Assembly.ReflectionOnlyLoad(args.Name);
                var list = new List<DriveServiceDescriptor>();
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext != ".dll" && ext != ".exe")
                            continue;

                        if (Path.GetFileName(file).StartsWith("ShellBoost.", StringComparison.OrdinalIgnoreCase))
                            continue;

                        Assembly asm;
                        try
                        {
                            asm = Assembly.ReflectionOnlyLoadFrom(file);
                        }
                        catch
                        {
                            continue;
                        }

                        // note iface comparison is by name, not by type
                        foreach (var type in asm.GetExportedTypes().Where(t => t.GetInterfaces().Any(i => i.FullName == typeof(IDriveService).FullName)))
                        {
                            list.Add(new DriveServiceDescriptor(type));
                        }
                    }
                }
                return list.ToArray();
            }
        }
    }
}
