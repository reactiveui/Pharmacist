// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.Metadata;

namespace Pharmacist.Core.Generation.Compilation
{
    internal static class PathSearchExtensions
    {
        /// <summary>
        /// Resolves the specified full assembly name.
        /// </summary>
        /// <param name="reference">A reference with details about the assembly.</param>
        /// <param name="targetAssemblyDirectories">The directories potentially containing the assemblies.</param>
        /// <param name="parameters">Parameters to provide to the reflection system..</param>
        /// <returns>The assembly definitions.</returns>
        public static IReadOnlyCollection<PEFile> Resolve(this IAssemblyReference reference, IReadOnlyCollection<string> targetAssemblyDirectories, PEStreamOptions parameters = PEStreamOptions.PrefetchMetadata)
        {
            var dllName = reference.Name + ".dll";

            var fullPaths = targetAssemblyDirectories.Select(x => Path.Combine(x, dllName)).Where(File.Exists).ToList();
            if (fullPaths.Count == 0)
            {
                dllName = reference.Name + ".winmd";
                fullPaths = targetAssemblyDirectories.Select(x => Path.Combine(x, dllName)).Where(File.Exists).ToList();
            }

            return fullPaths.Select(fullPath => new PEFile(fullPath, parameters)).ToList();
        }
    }
}
