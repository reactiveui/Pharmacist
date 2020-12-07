﻿// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Pharmacist.Core.Utilities
{
    internal static class AssemblyHelpers
    {
        private static readonly string[] AssemblyFileExtensions =
        {
            ".winmd", ".dll", ".exe"
        };

        public static ISet<string> AssemblyFileExtensionsSet { get; } = new HashSet<string>(AssemblyFileExtensions, StringComparer.InvariantCultureIgnoreCase);

        internal static string? FindUnionMetadataFile(string name, Version version)
        {
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits", "10", "UnionMetadata");

            if (!Directory.Exists(basePath))
            {
                return null;
            }

            basePath = Path.Combine(basePath, FindClosestVersionDirectory(basePath, version));

            if (!Directory.Exists(basePath))
            {
                return null;
            }

            var file = Path.Combine(basePath, name + ".winmd");

            return !File.Exists(file) ? null : file;
        }

        internal static string? FindWindowsMetadataFile(string name, Version version)
        {
            // This is only supported on windows at the moment.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Kits", "10", "References");

            if (!Directory.Exists(basePath))
            {
                return FindWindowsMetadataInSystemDirectory(name);
            }

            basePath = Path.Combine(basePath, FindClosestVersionDirectory(basePath, version));

            if (!Directory.Exists(basePath))
            {
                return FindWindowsMetadataInSystemDirectory(name);
            }

            var file = Path.Combine(basePath, name + ".winmd");

            return !File.Exists(file) ? FindWindowsMetadataInSystemDirectory(name) : file;
        }

        private static string? FindWindowsMetadataInSystemDirectory(string name)
        {
            var file = Path.Combine(Environment.SystemDirectory, "WinMetadata", name + ".winmd");
            return File.Exists(file) ? file : null;
        }

        private static string FindClosestVersionDirectory(string basePath, Version version)
        {
            var directory = new DirectoryInfo(basePath);
            string? path = directory.EnumerateDirectories().Select(d => ConvertToVersion(d.Name)).Where(x => x.Version is not null && x.Version == version).Select(x => x.Name).FirstOrDefault();

            return path ?? version.ToString();
        }

        [SuppressMessage("Design", "CA1031: Modify to catch a more specific exception type, or rethrow the exception.", Justification = "Deliberate usage.")]
        private static (Version? Version, string? Name) ConvertToVersion(string name)
        {
            string RemoveTrailingVersionInfo()
            {
                var shortName = name;
                var dashIndex = shortName.IndexOf('-');
                if (dashIndex > 0)
                {
                    shortName = shortName.Remove(dashIndex);
                }

                return shortName;
            }

            try
            {
                return (new Version(RemoveTrailingVersionInfo()), name);
            }
            catch (Exception)
            {
                return (null, null);
            }
        }
    }
}
