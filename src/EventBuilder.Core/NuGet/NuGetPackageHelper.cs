// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace EventBuilder.Core.NuGet
{
    /// <summary>
    /// A helper class for handling NuGet packages.
    /// </summary>
    public static class NuGetPackageHelper
    {
        // Bunch of NuGet based objects we can cache and only create once.
        private static readonly string _globalPackagesPath = SettingsUtility.GetGlobalPackagesFolder(new XPlatMachineWideSetting().Settings);
        private static readonly NuGetLogger _logger = new NuGetLogger();
        private static readonly PackageDownloadContext _downloadContext = new PackageDownloadContext(NullSourceCacheContext.Instance);
        private static readonly List<Lazy<INuGetResourceProvider>> _providers;

        static NuGetPackageHelper()
        {
            _providers = new List<Lazy<INuGetResourceProvider>>();
            _providers.AddRange(Repository.Provider.GetCoreV3());
        }

        /// <summary>
        /// Downloads the specified packages and returns the files and directories where the package NuGet package lives.
        /// </summary>
        /// <param name="packageIdentities">The identities of the packages to find.</param>
        /// <param name="supportPackageIdentities">Any support libraries where the directories should be included but not the files.</param>
        /// <param name="framework">Optional framework parameter which will force NuGet to evaluate as the specified Framework. If null it will use .NET Standard 2.0.</param>
        /// <param name="nugetSource">Optional v3 nuget source. Will default to default nuget.org servers.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The directory where the NuGet packages are unzipped to.</returns>
        public static async Task<IEnumerable<(string folder, IEnumerable<string> files)>> DownloadPackageAndGetLibFilesAndFolder(IEnumerable<PackageIdentity> packageIdentities, IEnumerable<PackageIdentity> supportPackageIdentities = null, NuGetFramework framework = null, PackageSource nugetSource = null, CancellationToken token = default)
        {
            // If the user hasn't selected a default framework to extract, select .NET Standard 2.0
            framework = framework ?? FrameworkConstants.CommonFrameworks.NetStandard20;

            // Get our support libraries together. We will grab the default for the framework passed in if it's packaged based.
            var defaultSupportLibrary = framework?.ToPackageIdentity() == null ? Enumerable.Empty<PackageIdentity>() : new[] { framework.ToPackageIdentity() };
            supportPackageIdentities = (supportPackageIdentities ?? Array.Empty<PackageIdentity>()).Concat(defaultSupportLibrary).Distinct();

            // Combine together the primary/secondary packages, boolean value to indicate if we should download.
            IEnumerable<(PackageIdentity packageIdentity, bool includeFiles)> packagesToDownload = packageIdentities.Select(x => (x, true))
                .Concat(supportPackageIdentities.Select(x => (x, false)));

            return await Task.WhenAll(packagesToDownload
                .Select(x => CopyPackageLibraryItems(x.packageIdentity, nugetSource, framework, x.includeFiles, token)))
                .ConfigureAwait(false);
        }

        private static async Task<(string folder, IEnumerable<string> files)> CopyPackageLibraryItems(PackageIdentity package, PackageSource nugetSource, NuGetFramework framework, bool includeFilesInOutput, CancellationToken token)
        {
            var directory = EnsureDirectory(package.ToString());

            // If the file already exists (since it has the version number), assumed it was already grabbed previously and use that.
            if (Directory.Exists(directory))
            {
                return (directory, Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories));
            }

            // Use the provided nuget package source, or use nuget.org
            var source = new SourceRepository(nugetSource ?? new PackageSource("https://api.nuget.org/v3/index.json"), _providers);

            // Get the download resource from the nuget client API. This is basically a DI locator.
            var downloadResource = await source.GetResourceAsync<DownloadResource>(token).ConfigureAwait(false);

            // Download the resource into the global packages path. We get a result which allows us to copy or do other operations based on the files.
            var downloadResults = await downloadResource.GetDownloadResourceResultAsync(
                                              package,
                                              _downloadContext,
                                              _globalPackagesPath,
                                              _logger,
                                              token).ConfigureAwait(false);

            if (downloadResults.Status != DownloadResourceResultStatus.Available && downloadResults.Status != DownloadResourceResultStatus.AvailableWithoutStream)
            {
                return default;
            }

            // Get all the folders in our lib and build directory of our nuget. These are the general contents we include in our projects.
            var groups = (await Task.WhenAll(downloadResults.PackageReader.GetLibItemsAsync(token), downloadResults.PackageReader.GetBuildItemsAsync(token)).ConfigureAwait(false)).SelectMany(x => x);

            // Select our groups that match our selected framework and have content.
            var groupFiles = groups.Where(x => !x.HasEmptyFolder && x.TargetFramework == framework).SelectMany(x => x.Items).ToList();

            // Extract the files, don't bother copying the XML file contents.
            var packageFileExtractor = new PackageFileExtractor(groupFiles, XmlDocFileSaveMode.Skip);

            // Copy the files to our extractor cache directory.
            var outputFiles = await downloadResults.PackageReader.CopyFilesAsync(directory, groupFiles, packageFileExtractor.ExtractPackageFile, _logger, token).ConfigureAwait(false);

            // Return the folder, if we aren't excluding files return all the assemblies.
            return (directory, includeFilesInOutput ? outputFiles.Where(x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) : Enumerable.Empty<string>());
        }

        private static string EnsureDirectory(string subDirectory)
        {
            var packageUnzipPath = Path.Combine(Path.GetTempPath(), "EventBuilder.NuGet", subDirectory);

            if (!Directory.Exists(packageUnzipPath))
            {
                Directory.CreateDirectory(packageUnzipPath);
            }

            return packageUnzipPath;
        }
    }
}
