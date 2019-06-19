// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.NuGet
{
    /// <summary>
    /// A helper class for handling NuGet packages.
    /// </summary>
    public static class NuGetPackageHelper
    {
        /// <summary>
        /// Gets the default nuget source.
        /// </summary>
        public const string DefaultNuGetSource = "https://api.nuget.org/v3/index.json";

        private static readonly int ProcessingCount = Environment.ProcessorCount;

        private static readonly string[] DefaultFoldersToGrab = { PackagingConstants.Folders.Lib, PackagingConstants.Folders.Build, PackagingConstants.Folders.Ref };

        // Bunch of NuGet based objects we can cache and only create once.
        private static readonly string _globalPackagesPath;
        private static readonly NuGetLogger _logger = new NuGetLogger();
        private static readonly SourceCacheContext _sourceCacheContext = NullSourceCacheContext.Instance;
        private static readonly PackageDownloadContext _downloadContext = new PackageDownloadContext(_sourceCacheContext);
        private static readonly IFrameworkNameProvider _frameworkNameProvider = DefaultFrameworkNameProvider.Instance;

        static NuGetPackageHelper()
        {
            Providers = new List<Lazy<INuGetResourceProvider>>();
            Providers.AddRange(Repository.Provider.GetCoreV3());

            var machineWideSettings = new XPlatMachineWideSetting();
            _globalPackagesPath = SettingsUtility.GetGlobalPackagesFolder(machineWideSettings.Settings.LastOrDefault() ?? (ISettings)NullSettings.Instance);
        }

        /// <summary>
        /// Gets the providers for the nuget resources.
        /// </summary>
        public static List<Lazy<INuGetResourceProvider>> Providers { get; }

        /// <summary>
        /// Downloads the specified packages and returns the files and directories where the package NuGet package lives.
        /// </summary>
        /// <param name="libraryIdentities">Library identities we want to match.</param>
        /// <param name="frameworks">Optional framework parameter which will force NuGet to evaluate as the specified Framework. If null it will use .NET Standard 2.0.</param>
        /// <param name="nugetSource">Optional v3 nuget source. Will default to default nuget.org servers.</param>
        /// <param name="getDependencies">If we should get the dependencies.</param>
        /// <param name="packageFolders">Directories to package folders. Will be lib/build/ref if not defined.</param>
        /// <param name="packageOutputDirectory">A directory where to store the files, if null a random location will be used.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The directory where the NuGet packages are unzipped to. Also the files contained within the requested package only.</returns>
        public static async Task<IReadOnlyCollection<(string folder, IReadOnlyCollection<string> files)>> DownloadPackageFilesAndFolder(
            IReadOnlyCollection<LibraryRange> libraryIdentities,
            IReadOnlyCollection<NuGetFramework> frameworks = null,
            PackageSource nugetSource = null,
            bool getDependencies = true,
            IReadOnlyCollection<string> packageFolders = null,
            string packageOutputDirectory = null,
            CancellationToken token = default)
        {
            // If the user hasn't selected a default framework to extract, select .NET Standard 2.0
            frameworks = frameworks ?? new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };

            // Use the provided nuget package source, or use nuget.org
            var sourceRepository = new SourceRepository(nugetSource ?? new PackageSource(DefaultNuGetSource), Providers);

            var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>(token).ConfigureAwait(false);
            var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(token).ConfigureAwait(false);

            var packages = await Task.WhenAll(libraryIdentities.Select(x => GetBestMatch(x, findPackageResource, token))).ConfigureAwait(false);

            return await DownloadPackageFilesAndFolder(packages, frameworks, downloadResource, getDependencies, packageFolders, packageOutputDirectory, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Downloads the specified packages and returns the files and directories where the package NuGet package lives.
        /// </summary>
        /// <param name="packageIdentities">The identity of the packages to find.</param>
        /// <param name="frameworks">Optional framework parameter which will force NuGet to evaluate as the specified Framework. If null it will use .NET Standard 2.0.</param>
        /// <param name="nugetSource">Optional v3 nuget source. Will default to default nuget.org servers.</param>
        /// <param name="getDependencies">If we should get the dependencies.</param>
        /// <param name="packageFolders">Directories to package folders. Will be lib/build/ref if not defined.</param>
        /// <param name="packageOutputDirectory">A directory where to store the files, if null a random location will be used.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The directory where the NuGet packages are unzipped to. Also the files contained within the requested package only.</returns>
        public static async Task<IReadOnlyCollection<(string folder, IReadOnlyCollection<string> files)>> DownloadPackageFilesAndFolder(
            IReadOnlyCollection<PackageIdentity> packageIdentities,
            IReadOnlyCollection<NuGetFramework> frameworks = null,
            PackageSource nugetSource = null,
            bool getDependencies = true,
            IReadOnlyCollection<string> packageFolders = null,
            string packageOutputDirectory = null,
            CancellationToken token = default)
        {
            // If the user hasn't selected a default framework to extract, select .NET Standard 2.0
            frameworks = frameworks ?? new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };

            // Use the provided nuget package source, or use nuget.org
            var sourceRepository = new SourceRepository(nugetSource ?? new PackageSource(DefaultNuGetSource), Providers);

            var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>(token).ConfigureAwait(false);

            return await DownloadPackageFilesAndFolder(packageIdentities, frameworks, downloadResource, getDependencies, packageFolders, packageOutputDirectory, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the best matching PackageIdentity for the specified LibraryRange.
        /// </summary>
        /// <param name="identity">The library range to find the best patch for.</param>
        /// <param name="findPackageResource">The source repository where to match.</param>
        /// <param name="token">A optional cancellation token.</param>
        /// <returns>The best matching PackageIdentity to the specified version range.</returns>
        public static async Task<PackageIdentity> GetBestMatch(LibraryRange identity, FindPackageByIdResource findPackageResource, CancellationToken token)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (findPackageResource == null)
            {
                throw new ArgumentNullException(nameof(findPackageResource));
            }

            var versions = await findPackageResource.GetAllVersionsAsync(identity.Name, _sourceCacheContext, _logger, token).ConfigureAwait(false);

            var bestPackageVersion = versions?.FindBestMatch(identity.VersionRange, version => version);

            return new PackageIdentity(identity.Name, bestPackageVersion);
        }

        /// <summary>
        /// Downloads the specified packages and returns the files and directories where the package NuGet package lives.
        /// </summary>
        /// <param name="packageIdentities">The identity of the packages to find.</param>
        /// <param name="frameworks">Framework parameter which will force NuGet to evaluate as the specified Framework. If null it will use .NET Standard 2.0.</param>
        /// <param name="downloadResource">The download resource.</param>
        /// <param name="getDependencies">If we should get the dependencies.</param>
        /// <param name="packageFolders">Directories to package folders. Will be lib/build/ref if not defined.</param>
        /// <param name="packageOutputDirectory">A directory where to store the files, if null a random location will be used.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The directory where the NuGet packages are unzipped to. Also the files contained within the requested package only.</returns>
        private static async Task<IReadOnlyCollection<(string folder, IReadOnlyCollection<string> files)>> DownloadPackageFilesAndFolder(
            IReadOnlyCollection<PackageIdentity> packageIdentities,
            IReadOnlyCollection<NuGetFramework> frameworks,
            DownloadResource downloadResource,
            bool getDependencies = true,
            IReadOnlyCollection<string> packageFolders = null,
            string packageOutputDirectory = null,
            CancellationToken token = default)
        {
            var librariesToCopy = await GetPackagesToCopy(packageIdentities, downloadResource, frameworks, getDependencies, token).ConfigureAwait(false);

            packageOutputDirectory = packageOutputDirectory ?? GetRandomPackageDirectory();
            packageFolders = packageFolders ?? DefaultFoldersToGrab;

            return CopyPackageFiles(librariesToCopy, frameworks, packageFolders, packageOutputDirectory, token).ToList();
        }

        private static async Task<IReadOnlyCollection<(DownloadResourceResult downloadResourceResult, PackageIdentity packageIdentity, bool includeFilesInOutput)>> GetPackagesToCopy(
            IReadOnlyCollection<PackageIdentity> startingPackages,
            DownloadResource downloadResource,
            IReadOnlyCollection<NuGetFramework> frameworks,
            bool getDependencies,
            CancellationToken token)
        {
            var packagesToCopy = new Dictionary<PackageIdentity, (DownloadResourceResult downloadResourceResult, PackageIdentity packageIdentity, bool includeFilesInOutput)>(PackageIdentityNameComparer.Default);

            var stack = new Stack<(PackageIdentity packageIdentity, bool include)>(startingPackages.Select(x => (x, true)));

            if (getDependencies)
            {
                var supportLibraries = frameworks.SelectMany(x => x.GetSupportLibraries()).ToArray();
                if (supportLibraries.Length > 0)
                {
                    stack.PushRange(supportLibraries.Select(x => (x, false)));
                }
            }

            var processingItems = new (PackageIdentity packageIdentity, bool includeFiles)[ProcessingCount];
            while (stack.Count != 0)
            {
                var count = stack.TryPopRange(processingItems);

                var currentItems = processingItems.Take(count).Where(
                    item =>
                    {
                        if (packagesToCopy.TryGetValue(item.packageIdentity, out var existingValue) && item.packageIdentity.Version <= existingValue.packageIdentity.Version)
                        {
                            return false;
                        }

                        return true;
                    }).ToList();

                // Download the resource into the global packages path. We get a result which allows us to copy or do other operations based on the files.
                (DownloadResourceResult downloadResourceResult, PackageIdentity packageIdentity, bool includeFilesInOutput)[] results = await Task.WhenAll(
                         currentItems.Select(
                             async item =>
                                 (await downloadResource.GetDownloadResourceResultAsync(item.packageIdentity, _downloadContext, _globalPackagesPath, _logger, token).ConfigureAwait(false), item.packageIdentity, item.includeFiles))).ConfigureAwait(false);

                foreach (var item in results.Where(x => x.downloadResourceResult.Status == DownloadResourceResultStatus.Available || x.downloadResourceResult.Status == DownloadResourceResultStatus.AvailableWithoutStream))
                {
                    packagesToCopy[item.packageIdentity] = item;
                    var dependencyInfos = GetDependencyPackages(item.downloadResourceResult, frameworks.First());

                    stack.PushRange(dependencyInfos.Select(x => (x, false)));
                }
            }

            return packagesToCopy.Values.ToList();
        }

        private static IEnumerable<(string folder, IReadOnlyCollection<string> files)> CopyPackageFiles(
            IReadOnlyCollection<(DownloadResourceResult downloadResourceResult, PackageIdentity packageIdentity, bool includeFilesInOutput)> packagesToProcess,
            IReadOnlyCollection<NuGetFramework> frameworks,
            IReadOnlyCollection<string> packageFolders,
            string packageDirectory,
            CancellationToken token)
        {
            // Default back to a any framework.
            if (!frameworks.Contains(NuGetFramework.AnyFramework))
            {
                frameworks = frameworks.Concat(new[] { NuGetFramework.AnyFramework }).ToList();
            }

            foreach (var packageToProcess in packagesToProcess)
            {
                var (downloadResourceResult, packageIdentity, includeFilesInOutput) = packageToProcess;
                var directory = Path.Combine(packageDirectory, packageIdentity.Id, packageIdentity.Version.ToNormalizedString());

                // Get all the folders in our lib and build directory of our nuget. These are the general contents we include in our projects.
                var group = downloadResourceResult.PackageReader.GetFileGroup(packageFolders, frameworks);

                // Select our groups that match our selected framework and have content.
                var groupFiles = group?.Items.ToArray() ?? Array.Empty<string>();

                if (groupFiles.Length == 0)
                {
                    continue;
                }

                // Extract the files, don't bother copying the XML file contents.
                var packageFileExtractor = new PackageFileExtractor(groupFiles, XmlDocFileSaveMode.Skip);

                EnsureDirectory(directory);

                // Copy the files to our extractor cache directory.
                var outputFiles = downloadResourceResult.PackageReader.CopyFiles(directory, groupFiles, packageFileExtractor.ExtractPackageFile, _logger, token).ToList();

                // Return the folder, if we aren't excluding files return all the assemblies.
                yield return (directory, includeFilesInOutput ? (IReadOnlyCollection<string>)outputFiles : Array.Empty<string>());
            }
        }

        private static string GetRandomPackageDirectory() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        private static void EnsureDirectory(string packageUnzipPath)
        {
            if (!Directory.Exists(packageUnzipPath))
            {
                Directory.CreateDirectory(packageUnzipPath);
            }
        }

        /// <summary>
        /// Gets dependency packages that matches our framework (current version or below).
        /// </summary>
        /// <param name="downloadResults">The results where to get the dependencies from.</param>
        /// <param name="framework">The framework to match dependencies for.</param>
        /// <returns>The dependencies, or an empty array if there are no dependencies.</returns>
        private static IEnumerable<PackageIdentity> GetDependencyPackages(DownloadResourceResult downloadResults, NuGetFramework framework)
        {
            // Grab the package dependency group that matches is closest to our framework.
            var highestFramework = downloadResults.PackageReader.GetPackageDependencies()
                .Where(dependency => NuGetFrameworkInRangeComparer.Default.Equals(framework, dependency.TargetFramework))
                .OrderByDescending(dependency => dependency.TargetFramework.Version)
                .FirstOrDefault();

            // If no packages match our framework just return an empty array.
            if (highestFramework == null)
            {
                return Array.Empty<PackageIdentity>();
            }

            return highestFramework.Packages.Select(package => new PackageIdentity(package.Id, package.VersionRange.MinVersion));
        }

        private static FrameworkSpecificGroup GetFileGroup(this PackageReaderBase reader, IEnumerable<string> folders, IReadOnlyCollection<NuGetFramework> frameworksToInclude)
        {
            var groups = new Dictionary<NuGetFramework, List<string>>(new NuGetFrameworkFullComparer());

            foreach (var folder in folders)
            {
                foreach (var file in reader.GetFiles(folder))
                {
                    var frameworkFromPath = reader.GetFrameworkFromPath(file, true);

                    var framework = frameworksToInclude.FirstOrDefault(x => NuGetFrameworkInRangeComparer.Default.Equals(x, frameworkFromPath));

                    if (framework == null)
                    {
                        continue;
                    }

                    if (!groups.TryGetValue(frameworkFromPath, out var stringList))
                    {
                        stringList = new List<string>();
                        groups.Add(frameworkFromPath, stringList);
                    }

                    stringList.Add(file);
                }
            }

            foreach (var targetFramework in frameworksToInclude)
            {
                var key = groups.Keys.Where(x => NuGetFrameworkInRangeComparer.Default.Equals(targetFramework, x)).OrderByDescending(x => x.Version).FirstOrDefault();

                if (key == null)
                {
                    continue;
                }

                if (!groups.TryGetValue(key, out var files))
                {
                    continue;
                }

                if (files.Count == 0)
                {
                    continue;
                }

                return new FrameworkSpecificGroup(targetFramework, files.ToList());
            }

            return null;
        }

        private static NuGetFramework GetFrameworkFromPath(this IPackageCoreReader reader, string path, bool allowSubFolders = false)
        {
            var nuGetFramework = NuGetFramework.AnyFramework;
            var strArray = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if ((strArray.Length == 3 || strArray.Length > 3) && allowSubFolders)
            {
                string folderName = strArray[1];
                NuGetFramework folder;
                try
                {
                    folder = NuGetFramework.ParseFolder(folderName, _frameworkNameProvider);
                }
                catch (ArgumentException ex)
                {
                    throw new PackagingException(string.Format(CultureInfo.CurrentCulture, "There is a invalid project {0}, {1}", path, reader.GetIdentity()), ex);
                }

                if (folder.IsSpecificFramework)
                {
                    nuGetFramework = folder;
                }
            }

            return nuGetFramework;
        }
    }
}
