// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;

using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.Extractors
{
    /// <summary>
    /// A extractor which will extract assembly information from a NuGet package.
    /// </summary>
    public class NuGetExtractor : IExtractor
    {
        /// <inheritdoc/>
        public List<string> Assemblies { get; } = new List<string>();

        /// <inheritdoc/>
        public List<string> SearchDirectories { get; } = new List<string>();

        /// <summary>
        /// Extracts the data using the specified target framework.
        /// </summary>
        /// <param name="targetFramework">The target framework to extract.</param>
        /// <param name="package">The package to extract the information from.</param>
        /// <param name="supportPackages">The packages for support purposes.</param>
        /// <returns>A task to monitor the progress.</returns>
        public async Task Extract(NuGetFramework targetFramework, PackageIdentity package, IEnumerable<PackageIdentity> supportPackages = null)
        {
            var results = (await NuGetPackageHelper.DownloadPackageAndGetLibFilesAndFolder(package, supportPackages, targetFramework).ConfigureAwait(false)).ToList();
            Assemblies.AddRange(results.SelectMany(x => x.files));
            SearchDirectories.AddRange(results.Select(x => x.folder));
        }
    }
}
