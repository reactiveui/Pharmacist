// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

using NuGet.Frameworks;

using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Generation.Compilation
{
    internal static class PathSearchExtensions
    {
        /// <summary>
        /// Resolves the specified full assembly name.
        /// </summary>
        /// <param name="reference">A reference with details about the assembly.</param>
        /// <param name="parent">The parent of the reference.</param>
        /// <param name="input">The directories potentially containing the assemblies.</param>
        /// <param name="framework">The framework we are processing for.</param>
        /// <param name="parameters">Parameters to provide to the reflection system..</param>
        /// <returns>The assembly definitions.</returns>
        public static PEFile? Resolve(this IAssemblyReference reference, IModule parent, InputAssembliesGroup input, NuGetFramework framework, PEStreamOptions parameters = PEStreamOptions.PrefetchMetadata)
        {
            var fileName = GetFileName(reference, parent, input, framework);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return new PEFile(fileName, parameters);
        }

        private static string? GetFileName(IAssemblyReference reference, IModule parent, InputAssembliesGroup input, NuGetFramework framework)
        {
            var extensions = reference.IsWindowsRuntime ? new[] { ".winmd", ".dll" } : new[] { ".exe", ".dll" };

            if (reference.IsWindowsRuntime)
            {
                return AssemblyHelpers.FindWindowsMetadataFile(reference.Name, reference.Version);
            }

            string? file;

            if (reference.Name == "mscorlib")
            {
                file = GetCorlib(reference);
                if (!string.IsNullOrWhiteSpace(file))
                {
                    return file;
                }
            }

            file = FindInParentDirectory(reference, parent, extensions);

            if (!string.IsNullOrWhiteSpace(file))
            {
                return file;
            }

            file = SearchDirectories(reference, input, extensions);

            if (!string.IsNullOrWhiteSpace(file))
            {
                return file;
            }

            return SearchNugetFrameworkDirectories(reference, extensions, framework);
        }

        private static string? FindInParentDirectory(IAssemblyReference reference, IModule parent, IEnumerable<string> extensions)
        {
            if (parent == null)
            {
                return null;
            }

            var baseDirectory = Path.GetDirectoryName(parent.PEFile.FileName);

            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                return null;
            }

            foreach (var extension in extensions)
            {
                var moduleFileName = Path.Combine(baseDirectory, reference.Name + extension);
                if (!File.Exists(moduleFileName))
                {
                    continue;
                }

                return moduleFileName;
            }

            return null;
        }

        private static string? SearchNugetFrameworkDirectories(IAssemblyReference reference, IReadOnlyCollection<string> extensions, NuGetFramework framework)
        {
            var folders = framework.GetNuGetFrameworkFolders();

            foreach (var folder in folders)
            {
                foreach (var extension in extensions)
                {
                    var testName = Path.Combine(folder, reference.Name + extension);

                    if (string.IsNullOrWhiteSpace(testName))
                    {
                        continue;
                    }

                    if (!File.Exists(testName))
                    {
                        continue;
                    }

                    return testName;
                }
            }

            return null;
        }

        private static string? SearchDirectories(IAssemblyReference name, InputAssembliesGroup input, IEnumerable<string> extensions)
        {
            foreach (var extension in extensions)
            {
                var testName = input.SupportGroup.GetFullFilePath(name.Name + extension);
                if (string.IsNullOrWhiteSpace(testName))
                {
                    continue;
                }

                if (!File.Exists(testName))
                {
                    continue;
                }

                return testName;
            }

            return null;
        }

        private static string? GetCorlib(IAssemblyReference reference)
        {
            var version = reference.Version;
            var corlib = typeof(object).Assembly.GetName();

            if (corlib.Version == version || IsSpecialVersionOrRetargetable(reference))
            {
                return typeof(object).Module.FullyQualifiedName;
            }

            string? path;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = GetMscorlibBasePath(version, reference.PublicKeyToken.ToHexString(8));
            }
            else
            {
                path = GetMonoMscorlibBasePath(version);
            }

            if (path == null)
            {
                return null;
            }

            var file = Path.Combine(path, "mscorlib.dll");
            if (File.Exists(file))
            {
                return file;
            }

            return null;
        }

        private static string? GetMscorlibBasePath(Version version, string publicKeyToken)
        {
            string? GetSubFolderForVersion()
            {
                switch (version.Major)
                {
                    case 1:
                        if (version.MajorRevision == 3300)
                        {
                            return "v1.0.3705";
                        }

                        return "v1.1.4322";
                    case 2:
                        return "v2.0.50727";
                    case 4:
                        return "v4.0.30319";
                    default:
                        return null;
                }
            }

            if (publicKeyToken == "969db8053d3322ac")
            {
                var programFiles = Environment.Is64BitOperatingSystem ?
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) :
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var windowsCeDirectoryPath = $@"Microsoft.NET\SDK\CompactFramework\v{version.Major}.{version.Minor}\WindowsCE\";
                var fullDirectoryPath = Path.Combine(programFiles, windowsCeDirectoryPath);
                if (Directory.Exists(fullDirectoryPath))
                {
                    return fullDirectoryPath;
                }
            }
            else
            {
                var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET");
                string[] frameworkPaths =
                {
                    Path.Combine(rootPath, "Framework"),
                    Path.Combine(rootPath, "Framework64")
                };

                var folder = GetSubFolderForVersion();

                if (folder != null)
                {
                    foreach (var path in frameworkPaths)
                    {
                        var basePath = Path.Combine(path, folder);
                        if (Directory.Exists(basePath))
                        {
                            return basePath;
                        }
                    }
                }
            }

            return null;
        }

        private static string? GetMonoMscorlibBasePath(Version version)
        {
            var path = Directory.GetParent(typeof(object).Module.FullyQualifiedName).Parent?.FullName;

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (version.Major == 1)
            {
                path = Path.Combine(path, "1.0");
            }
            else if (version.Major == 2)
            {
                if (version.MajorRevision == 5)
                {
                    path = Path.Combine(path, "2.1");
                }
                else
                {
                    path = Path.Combine(path, "2.0");
                }
            }
            else if (version.Major == 4)
            {
                path = Path.Combine(path, "4.0");
            }

            if (Directory.Exists(path))
            {
                return path;
            }

            return null;
        }

        private static bool IsSpecialVersionOrRetargetable(IAssemblyReference reference)
        {
            return IsZeroOrAllOnes(reference.Version) || reference.IsRetargetable;
        }

        private static bool IsZeroOrAllOnes(Version version)
        {
            return version == null
                   || (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0)
                   || (version.Major == 65535 && version.Minor == 65535 && version.Build == 65535 && version.Revision == 65535);
        }
    }
}
