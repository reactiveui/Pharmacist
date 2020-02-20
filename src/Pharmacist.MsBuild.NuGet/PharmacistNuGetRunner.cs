// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NuGet.LibraryModel;

using Pharmacist.Core;
using Pharmacist.MsBuild.Common;
using Serilog;
using Splat;
using Splat.Serilog;

namespace Pharmacist.MsBuild.NuGet
{
    /// <summary>
    /// A task for generating events.
    /// </summary>
    [SuppressMessage("Design", "CA1031: Catch specific exceptions", Justification = "Final logging location for exceptions.")]
    public static class PharmacistNuGetRunner
    {
        private static readonly ISet<string> ExclusionPackageReferenceSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "Pharmacist.MSBuild",
            "Pharmacist.Common"
        };

        /// <summary>
        /// Executes the logic.
        /// </summary>
        /// <param name="options">The options to execute.</param>
        /// <returns>If we were successful.</returns>
        public static int Execute(AppOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var jsonSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            Locator.CurrentMutable.UseSerilogFullLogger();
            var logger = Log.Logger;
            if (string.IsNullOrWhiteSpace(options.OutputFile))
            {
                logger.Error($"{nameof(options.OutputFile)} is not set");
                return 1;
            }

            var nugetFrameworks = TargetFrameworkHelper.GetTargetFrameworks(options.TargetFramework, options.TargetFrameworkVersion, options.TargetPlatformVersion, options.ProjectTypeGuids);
            if (nugetFrameworks == null)
            {
                logger.Error("Neither TargetFramework nor ProjectTypeGuids have been correctly set.");
                return 1;
            }

            var (includeFileNames, supportFileNames, packages) = GetPackages(options.PackageReferences, options.ProjectAssetFile);

            var lockFile = options.OutputFile + ".lock";

            if (File.Exists(lockFile) && File.Exists(options.OutputFile))
            {
                try
                {
                    var fileContents = File.ReadAllText(lockFile);
                    var lockedLibraries = JsonConvert.DeserializeObject<List<LibraryRange>>(fileContents, jsonSettings);
                    if (lockedLibraries != null && lockedLibraries.Count == packages.Count && lockedLibraries.All(packages.Contains))
                    {
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Could not read lock file.");
                }
            }

            using (var writer = new StreamWriter(Path.Combine(options.OutputFile)))
            {
                ObservablesForEventGenerator.WriteHeader(writer, packages).ConfigureAwait(false).GetAwaiter().GetResult();

                try
                {
                    ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(writer, packages, nugetFrameworks).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Cannot generate observables.");
                    return 1;
                }
            }

            File.WriteAllText(lockFile, JsonConvert.SerializeObject(packages, jsonSettings));
            return 0;
        }

        private static (IEnumerable<string> includeFileNames, IEnumerable<string> supportFileNames, IReadOnlyCollection<LibraryRange> librariesIncluded) GetPackages(IEnumerable<string> packageReferences, string dependencyFile)
        {
            using var dependencyReader = new DependencyContextJsonReader();

            var dependencyContext = dependencyReader.Read(new FileStream(dependencyFile, FileMode.Open, FileAccess.Read));

            foreach (var runtime in dependencyContext.CompileLibraries)
            {
                runtime.ResolveReferencePaths();
            }

            return (null, null, null);
        }
    }
}
