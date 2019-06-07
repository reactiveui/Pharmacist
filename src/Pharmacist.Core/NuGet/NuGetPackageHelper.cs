// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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

namespace Pharmacist.Core.NuGet
{
    /// <summary>
    /// A helper class for handling NuGet packages.
    /// </summary>
    public static class NuGetPackageHelper
    {
        private const int ProcessingCount = 32;
        private static readonly string[] DefaultFoldersToGrab = { PackagingConstants.Folders.Lib, PackagingConstants.Folders.Build, PackagingConstants.Folders.Ref };

        // Bunch of NuGet based objects we can cache and only create once.
        private static readonly string _globalPackagesPath;
        private static readonly NuGetLogger _logger = new NuGetLogger();
        private static readonly PackageDownloadContext _downloadContext = new PackageDownloadContext(NullSourceCacheContext.Instance);
        private static readonly List<Lazy<INuGetResourceProvider>> _providers;

        private static readonly IFrameworkNameProvider _frameworkNameProvider = DefaultFrameworkNameProvider.Instance;

        static NuGetPackageHelper()
        {
            _providers = new List<Lazy<INuGetResourceProvider>>();
            _providers.AddRange(Repository.Provider.GetCoreV3());

            var machineWideSettings = new XPlatMachineWideSetting();
            _globalPackagesPath = SettingsUtility.GetGlobalPackagesFolder(machineWideSettings.Settings.LastOrDefault() ?? (ISettings)NullSettings.Instance);
        }

        /// <summary>
        /// Gets the directory where the packages will be stored.
        /// </summary>
        public static string PackageDirectory { get; } = Path.Combine(Path.GetTempPath(), "EventBuilder.NuGet");

        /// <summary>
        /// Downloads the specified packages and returns the files and directories where the package NuGet package lives.
        /// </summary>
        /// <param name="packageIdentities">The identity of the packages to find.</param>
        /// <param name="frameworks">Optional framework parameter which will force NuGet to evaluate as the specified Framework. If null it will use .NET Standard 2.0.</param>
        /// <param name="nugetSource">Optional v3 nuget source. Will default to default nuget.org servers.</param>
        /// <param name="getDependencies">If we should get the dependencies.</param>
        /// <param name="packageFolders">Directories to package folders. Will be lib/build/ref if not defined.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The directory where the NuGet packages are unzipped to. Also the files contained within the requested package only.</returns>
        public static async Task<IReadOnlyCollection<(string folder, IReadOnlyCollection<string> files)>> DownloadPackageAndFilesAndFolder(
            IReadOnlyCollection<PackageIdentity> packageIdentities,
            IReadOnlyCollection<NuGetFramework> frameworks = null,
            PackageSource nugetSource = null,
            bool getDependencies = true,
            IReadOnlyCollection<string> packageFolders = null,
            CancellationToken token = default)
        {
            // If the user hasn't selected a default framework to extract, select .NET Standard 2.0
            frameworks = frameworks ?? new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };

            // Use the provided nuget package source, or use nuget.org
            var source = new SourceRepository(nugetSource ?? new PackageSource("https://api.nuget.org/v3/index.json"), _providers);

            var librariesToCopy = await GetPackagesToCopy(packageIdentities, source, frameworks.First(), getDependencies, token).ConfigureAwait(false);

            return CopyPackageFiles(librariesToCopy, frameworks, packageFolders ?? DefaultFoldersToGrab, token);
        }

        private static async Task<IEnumerable<(PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult, bool includeFilesInOutput)>> GetPackagesToCopy(
            IReadOnlyCollection<PackageIdentity> startingPackages,
            SourceRepository source,
            NuGetFramework framework,
            bool getDependencies,
            CancellationToken token)
        {
            // Get the download resource from the nuget client API. This is basically a DI locator.
            var downloadResource = source.GetResource<DownloadResource>(token);

            var packagesToCopy = new Dictionary<string, (PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult, bool includeFilesInOutput)>(StringComparer.InvariantCultureIgnoreCase);

            var stack = new ConcurrentStack<PackageIdentity>(startingPackages);

            if (getDependencies)
            {
                var supportLibraries = framework.GetSupportLibraries().ToArray();
                if (supportLibraries.Length > 0)
                {
                    stack.PushRange(supportLibraries);
                }
            }

            var currentItems = new PackageIdentity[ProcessingCount];
            while (!stack.IsEmpty)
            {
                var count = stack.TryPopRange(currentItems);

                // Download the resource into the global packages path. We get a result which allows us to copy or do other operations based on the files.
                (DownloadResourceResult downloadResourceResult, PackageIdentity packageIdentity, bool includeFilesInOutput)[] results = await Task.WhenAll(currentItems.Take(count).Select(async item =>
                                          (await downloadResource.GetDownloadResourceResultAsync(item, _downloadContext, _globalPackagesPath, _logger, token).ConfigureAwait(false), item, startingPackages.Contains(item))))
                                          .ConfigureAwait(false);

                foreach (var result in results.Where(x => x.downloadResourceResult.Status == DownloadResourceResultStatus.Available || x.downloadResourceResult.Status == DownloadResourceResultStatus.AvailableWithoutStream))
                {
                    packagesToCopy[result.packageIdentity.Id] = (result.packageIdentity, result.downloadResourceResult, result.includeFilesInOutput);
                }

                if (getDependencies)
                {
                    var dependencies = results.SelectMany(x => GetDependencyPackages(x.downloadResourceResult, framework)
                        .Where(
                            dependentPackage =>
                            {
                                if (!packagesToCopy.TryGetValue(dependentPackage.Id, out var value))
                                {
                                    return true;
                                }

                                return dependentPackage.Version > value.packageIdentity.Version;
                            })).ToArray();

                    if (dependencies.Length > 0)
                    {
                        stack.PushRange(dependencies);
                    }
                }
            }

            return packagesToCopy.Select(x => (x.Value.packageIdentity, x.Value.downloadResourceResult, x.Value.includeFilesInOutput));
        }

        private static IReadOnlyCollection<(string folder, IReadOnlyCollection<string> files)> CopyPackageFiles(IEnumerable<(PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult, bool includeFilesInOutput)> packagesToProcess, IReadOnlyCollection<NuGetFramework> frameworks, IReadOnlyCollection<string> packageFolders, CancellationToken token)
        {
            var output = new List<(string folder, IReadOnlyCollection<string> files)>();
            foreach (var packageToProcess in packagesToProcess)
            {
                var (packageIdentity, downloadResourceResults, includeFilesInOutput) = packageToProcess;
                var directory = Path.Combine(PackageDirectory, packageIdentity.Id, packageIdentity.Version.ToNormalizedString());

                EnsureDirectory(directory);

                // Get all the folders in our lib and build directory of our nuget. These are the general contents we include in our projects.
                var groups = packageFolders.SelectMany(x => downloadResourceResults.PackageReader.GetFileGroups(x)).ToList();

                foreach (var framework in frameworks)
                {
                    // Select our groups that match our selected framework and have content.
                    var groupFiles = groups.Where(x => !x.HasEmptyFolder && x.TargetFramework.EqualToOrLessThan(framework)).OrderByDescending(x => x.TargetFramework.Version).FirstOrDefault()?.Items.ToArray() ?? Array.Empty<string>();

                    // Extract the files, don't bother copying the XML file contents.
                    var packageFileExtractor = new PackageFileExtractor(groupFiles, XmlDocFileSaveMode.Skip);

                    // Copy the files to our extractor cache directory.
                    var outputFiles = downloadResourceResults.PackageReader.CopyFiles(directory, groupFiles, packageFileExtractor.ExtractPackageFile, _logger, token)
                        .ToList();

                    if (outputFiles.Count > 0)
                    {
                        // Return the folder, if we aren't excluding files return all the assemblies.
                        output.Add((directory, includeFilesInOutput ? (IReadOnlyCollection<string>)outputFiles : Array.Empty<string>()));
                        break;
                    }
                }
            }

            return output;
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
                .Where(dependency => dependency.TargetFramework.EqualToOrLessThan(framework))
                .OrderByDescending(dependency => dependency.TargetFramework.Version)
                .FirstOrDefault();

            // If no packages match our framework just return an empty array.
            if (highestFramework == null)
            {
                return Array.Empty<PackageIdentity>();
            }

            return highestFramework.Packages.Select(package => new PackageIdentity(package.Id, package.VersionRange.MinVersion));
        }

        private static void EnsureDirectory(string packageUnzipPath)
        {
            if (!Directory.Exists(packageUnzipPath))
            {
                Directory.CreateDirectory(packageUnzipPath);
            }
        }

        private static bool EqualToOrLessThan(this NuGetFramework firstFramework, NuGetFramework secondFramework)
        {
            if (!NuGetFramework.FrameworkNameComparer.Equals(firstFramework, secondFramework))
            {
                return false;
            }

            return firstFramework.Version <= secondFramework.Version;
        }

        private static IEnumerable<FrameworkSpecificGroup> GetFileGroups(this PackageReaderBase reader, string folder)
        {
            var groups = new Dictionary<NuGetFramework, List<string>>(new NuGetFrameworkFullComparer());
            foreach (string file in reader.GetFiles(folder))
            {
                var frameworkFromPath = reader.GetFrameworkFromPath(file, true);
                if (!groups.TryGetValue(frameworkFromPath, out var stringList))
                {
                    stringList = new List<string>();
                    groups.Add(frameworkFromPath, stringList);
                }

                stringList.Add(file);
            }

            foreach (var targetFramework in groups.Keys.OrderBy(e => e, new NuGetFrameworkSorter()))
            {
                yield return new FrameworkSpecificGroup(targetFramework, groups[targetFramework].OrderBy(e => e, StringComparer.OrdinalIgnoreCase));
            }
        }

        private static NuGetFramework GetFrameworkFromPath(this IPackageCoreReader reader, string path, bool allowSubFolders = false)
        {
            var nuGetFramework = NuGetFramework.AnyFramework;
            var strArray = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
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
