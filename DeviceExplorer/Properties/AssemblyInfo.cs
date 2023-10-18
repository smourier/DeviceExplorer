using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyTitle("Device Explorer")]
[assembly: AssemblyDescription("Device Explorer")]
[assembly: AssemblyCompany("Simon Mourier")]
[assembly: AssemblyProduct("Device Explorer")]
[assembly: AssemblyCopyright("Copyright (C) 2021-2023 Simon Mourier. All rights reserved.")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("17aa5035-ddc4-45ca-98b3-2a3182a61747")]
[assembly: SupportedOSPlatform("windows10.0.22000.0")]

[assembly: AssemblyVersion("2.3.0.1")]
[assembly: AssemblyFileVersion("2.3.0.1")]
[assembly: AssemblyInformationalVersion("2.3.0.1")]
