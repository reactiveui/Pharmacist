﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Frameworks;
using NuGet.LibraryModel;
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
        private const string DefaultTargetFramework = "netstandard2.0";

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

            using (var writer = new StreamWriter(Path.Combine(OutputFile)))
            {
                var packages = GetPackages();

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

            return true;
        }

        private IReadOnlyCollection<NuGetFramework> GetTargetFrameworks()
        {
            IReadOnlyCollection<NuGetFramework> nugetFrameworks;
            if (!string.IsNullOrWhiteSpace(TargetFramework))
            {
                nugetFrameworks = TargetFramework.ToFrameworks();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ProjectTypeGuids) || string.IsNullOrWhiteSpace(TargetFrameworkVersion))
                {
                    return null;
                }

                var splitProjectTypeGuids = ProjectTypeGuids
                    .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => new Guid(x.Trim()));

                nugetFrameworks = new List<NuGetFramework> { splitProjectTypeGuids.GetTargetFramework(TargetFrameworkVersion) };
            }

            return nugetFrameworks;
        }

        private List<LibraryRange> GetPackages()
        {
            var packages = new List<LibraryRange>();

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

                var packageIdentity = new LibraryRange(include, nuGetVersion, LibraryDependencyTarget.Package);
                packages.Add(packageIdentity);
            }

            return packages;
        }
    }
}
