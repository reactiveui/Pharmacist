// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pharmacist.Core.Utilities
{
    internal static class FileSystemHelpers
    {
        public static IEnumerable<string> GetSubdirectoriesWithMatch(IEnumerable<string> directories, ISet<string> extensions)
        {
            var searchStack = new Stack<DirectoryInfo>(directories.Select(x => new DirectoryInfo(x)));

            while (searchStack.Count != 0)
            {
                var directoryInfo = searchStack.Pop();

                if (directoryInfo.EnumerateFiles().Any(file => extensions.Contains(file.Extension)))
                {
                    yield return directoryInfo.FullName;
                }

                foreach (var directory in directoryInfo.EnumerateDirectories())
                {
                    searchStack.Push(directory);
                }
            }
        }

        public static IEnumerable<string> GetFilesWithinSubdirectories(IEnumerable<string> directories)
        {
            return GetFilesWithinSubdirectories(directories, AssemblyHelpers.AssemblyFileExtensionsSet);
        }

        public static IEnumerable<string> GetFilesWithinSubdirectories(IEnumerable<string> directories, ISet<string> extensions)
        {
            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            var searchStack = new Stack<DirectoryInfo>(directories.Select(x => new DirectoryInfo(x)));

            while (searchStack.Count != 0)
            {
                var directoryInfo = searchStack.Pop();

                foreach (var file in directoryInfo.EnumerateFiles().Where(file => extensions.Contains(file.Extension)))
                {
                    yield return file.FullName;
                }

                foreach (var directory in directoryInfo.EnumerateDirectories())
                {
                    searchStack.Push(directory);
                }
            }
        }
    }
}
