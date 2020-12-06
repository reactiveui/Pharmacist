// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;

using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.Extractors
{
    /// <summary>
    /// A extractor which will extract assembly information from a NuGet package.
    /// </summary>
    public class NuGetExtractor : IExtractor
    {
        /// <inheritdoc />
        public InputAssembliesGroup? Input { get; protected set; }

        /// <summary>
        /// Extracts the data using the specified target framework.
        /// </summary>
        /// <param name="targetFrameworks">The target framework to extract in order of priority.</param>
        /// <param name="packages">The packages to extract the information from.</param>
        /// <param name="packageOutputDirectory">Directory for the packages, if null a random path in the temp folder will be used.</param>
        /// <returns>A task to monitor the progress.</returns>
        public async Task Extract(IReadOnlyCollection<NuGetFramework> targetFrameworks, IReadOnlyCollection<PackageIdentity> packages, string? packageOutputDirectory)
        {
            Input = await NuGetPackageHelper.DownloadPackageFilesAndFolder(packages, targetFrameworks, packageOutputDirectory: packageOutputDirectory).ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the data using the specified target framework.
        /// </summary>
        /// <param name="targetFrameworks">The target framework to extract in order of priority.</param>
        /// <param name="packages">The packages to extract the information from.</param>
        /// <param name="packageOutputDirectory">Directory for the packages, if null a random path in the temp folder will be used.</param>
        /// <returns>A task to monitor the progress.</returns>
        public async Task Extract(IReadOnlyCollection<NuGetFramework> targetFrameworks, IReadOnlyCollection<LibraryRange> packages, string? packageOutputDirectory)
        {
            Input = await NuGetPackageHelper.DownloadPackageFilesAndFolder(packages, targetFrameworks, packageOutputDirectory: packageOutputDirectory).ConfigureAwait(false);
        }
    }
}
