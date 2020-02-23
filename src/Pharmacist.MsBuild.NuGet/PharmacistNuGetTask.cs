// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core;
using Pharmacist.Core.NuGet;

using Splat;

namespace Pharmacist.MsBuild.NuGet
{
    /// <summary>
    /// A task for generating events.
    /// </summary>
    [SuppressMessage("Design", "CA1031: Catch specific exceptions", Justification = "Final logging location for exceptions.")]
    public class PharmacistNuGetTask : Task, IEnableLogger
    {
        private static readonly Regex _versionRegex = new Regex(@"(\d+\.)?(\d+\.)?(\d+\.)?(\*|\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _packageRegex = new Regex("^(.*)/(.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Dictionary<Guid, string> _guidToFramework = new Dictionary<Guid, string>()
        {
            [new Guid("EFBA0AD7-5A72-4C68-AF49-83D382785DCF")] = "MonoAndroid",
            [new Guid("6BC8ED88-2882-458C-8E55-DFD12B67127B")] = "Xamarin.iOS",
            [new Guid("A5A43C5B-DE2A-4C0C-9213-0A381AF9435A")] = "uap",
            [new Guid("A3F8F2AB-B479-4A4A-A458-A89E7DC349F1")] = "Xamarin.Mac",
        };

        private static readonly ISet<string> ExclusionPackageReferenceSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "Pharmacist.MSBuild",
            "Pharmacist.Common"
        };

        /// <summary>
        /// Gets or sets the project references.
        /// </summary>
        [Required]
        public ITaskItem[] PackageReferences { get; set; }

        /// <summary>
        /// Gets or sets the guids of the project types.
        /// </summary>
        public string ProjectTypeGuids { get; set; }

        /// <summary>
        /// Gets or sets the version of the project type.
        /// </summary>
        public string TargetFrameworkVersion { get; set; }

        /// <summary>
        /// Gets or sets the version of the project type associated with UWP projects.
        /// </summary>
        public string TargetPlatformVersion { get; set; }

        /// <summary>
        /// Gets or sets the target framework.
        /// </summary>
        public string TargetFramework { get; set; }

        /// <summary>
        /// Gets or sets the output file.
        /// </summary>
        [Required]
        public string OutputFile { get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            var lockFile = OutputFile + ".lock";
            var funcLogManager = new FuncLogManager(type => new WrappingFullLogger(new WrappingPrefixLogger(new MsBuildLogger(Log, LogLevel.Debug), type)));
            Locator.CurrentMutable.RegisterConstant(funcLogManager, typeof(ILogManager));

            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                Log.LogError($"{nameof(OutputFile)} is not set");
                return false;
            }

            var nugetFrameworks = GetTargetFrameworks();
            if (nugetFrameworks == null)
            {
                Log.LogError("Neither TargetFramework nor ProjectTypeGuids have been correctly set.");
                return false;
            }

            var packages = GetPackages();

            var lockPackages = ReadPackages(lockFile);

            if (lockPackages != null && lockPackages.Count == packages.Count && lockPackages.All(packages.Contains))
            {
                return true;
            }

            using (var writer = new StreamWriter(Path.Combine(OutputFile), false, Encoding.UTF8))
            {
                ObservablesForEventGenerator.WriteHeader(writer, packages).ConfigureAwait(false).GetAwaiter().GetResult();

                try
                {
                    ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(writer, packages, nugetFrameworks).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    this.Log().Error(ex);
                    return false;
                }
            }

            WritePackages(packages, lockFile);

            return true;
        }

        private IReadOnlyCollection<NuGetFramework> GetTargetFrameworks()
        {
            if (!string.IsNullOrWhiteSpace(TargetFramework))
            {
                return TargetFramework.ToFrameworks();
            }

            var nugetFrameworks = new List<NuGetFramework>();

            if (string.IsNullOrWhiteSpace(ProjectTypeGuids))
            {
                return null;
            }

            var projectGuids = ProjectTypeGuids
                .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => new Guid(x.Trim()));

            var versionText = string.IsNullOrWhiteSpace(TargetFrameworkVersion) ? TargetFrameworkVersion : TargetPlatformVersion;
            foreach (var projectGuid in projectGuids)
            {
                if (_guidToFramework.TryGetValue(projectGuid, out var targetFrameworkValue))
                {
                    var versionMatch = new Version(_versionRegex.Match(versionText).Value);
                    nugetFrameworks.Add(new NuGetFramework(targetFrameworkValue, versionMatch));
                }
            }

            return nugetFrameworks;
        }

        private List<PackageIdentity> GetPackages()
        {
            var packages = new List<PackageIdentity>();

            // Include all package references that aren't ourselves.
            foreach (var packageReference in PackageReferences)
            {
                var include = packageReference.GetMetadata("PackageName");

                if (ExclusionPackageReferenceSet.Contains(include))
                {
                    continue;
                }

                if (!VersionRange.TryParse(packageReference.GetMetadata("Version"), out var nuGetVersion))
                {
                    this.Log().Error($"Package {include} does not have a valid Version.");
                    continue;
                }

                var libraryRange = new LibraryRange(include, nuGetVersion, LibraryDependencyTarget.Package);
                var packageIdentity = NuGetPackageHelper.GetBestMatch(libraryRange).GetAwaiter().GetResult();
                packages.Add(packageIdentity);
            }

            return packages;
        }

        private void WritePackages(List<PackageIdentity> packageIdentities, string lockFileName)
        {
            using var streamWriter = new StreamWriter(lockFileName, false, Encoding.UTF8);
            foreach (var packageIdentity in packageIdentities)
            {
                streamWriter.WriteLine($"{packageIdentity.Id}/{packageIdentity.Version}");
            }
        }

        private List<PackageIdentity> ReadPackages(string lockFileName)
        {
            if (string.IsNullOrWhiteSpace(lockFileName))
            {
                throw new ArgumentException("Cannot have a empty lock file name", nameof(lockFileName));
            }

            var packageIdentities = new List<PackageIdentity>();

            if (File.Exists(lockFileName) == false)
            {
                return packageIdentities;
            }

            try
            {
                using var streamReader = new StreamReader(lockFileName, Encoding.UTF8, true);

                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var match = _packageRegex.Match(line);

                    var packageIdentity = new PackageIdentity(match.Groups[1].Value, NuGetVersion.Parse(match.Groups[2].Value));

                    packageIdentities.Add(packageIdentity);
                }
            }
            catch
            {
                packageIdentities = new List<PackageIdentity>();
            }

            return packageIdentities;
        }
    }
}
