using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

#if DEBUG
[assembly: AssemblyTitle("AmalgaDrive - Debug")]
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyTitle("AmalgaDrive - Release")]
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyDescription("A Windows Shell Extension that amalgamates various Cloud drives.")]
[assembly: AssemblyCompany("Aelyo Softworks")]
[assembly: AssemblyProduct("Amalga Drive")]
[assembly: AssemblyCopyright("Copyright (C) 2017-2019 Aelyo Softworks. All rights reserved.")]
[assembly: AssemblyTrademark("AmalgaDrive (TM) is a trademark of Aelyo Softworks.")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("b0b6cc61-d9e4-42ad-9a27-6dc2504e2bb7")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0.0")]

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
