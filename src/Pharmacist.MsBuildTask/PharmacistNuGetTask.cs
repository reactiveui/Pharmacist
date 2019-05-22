// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core;
using Pharmacist.Core.NuGet;

using Splat;

namespace Pharmacist.MsBuildTask
{
    /// <summary>
    /// A task for generating events.
    /// </summary>
    public class PharmacistNuGetTask : Task, IEnableLogger
    {
        /// <summary>
        /// Gets or sets the project references.
        /// </summary>
        [Required]
        public ITaskItem[] PackageReferences { get; set; }

        /// <summary>
        /// Gets or sets the target framework.
        /// </summary>
        [Required]
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

            if (string.IsNullOrWhiteSpace(TargetFramework))
            {
                Log.LogError($"{nameof(TargetFramework)} is not set");
                return false;
            }

            var taskList = new List<System.Threading.Tasks.Task>();
            using (var stream = new FileStream(Path.Combine(OutputFile), FileMode.Create, FileAccess.Write))
            {
                // Incldue all package references that aren't ourselves.
                foreach (var projectReference in PackageReferences.Where(x => !x.ItemSpec.Equals("Pharmacist.MSBuildTask", StringComparison.InvariantCultureIgnoreCase)))
                {
                    var include = projectReference.ItemSpec;

                    if (!NuGetVersion.TryParse(projectReference.GetMetadata("Version"), out var nuGetVersion))
                    {
                        this.Log().Error($"Package {include} does not have a valid Version.");
                        continue;
                    }

                    try
                    {
                        var packageIdentity = new PackageIdentity(include, nuGetVersion);
                        var nugetFramework = TargetFramework.ToFramework();
                        taskList.Add(ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(stream, packageIdentity, nugetFramework));
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error(ex);
                        return false;
                    }
                }

                System.Threading.Tasks.Task.WaitAll(taskList.ToArray());
            }

            return true;
        }
    }
}
