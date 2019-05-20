// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
        public ITaskItem[] ProjectReferences { get; set; }

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

            foreach (var projectReference in ProjectReferences)
            {
                Log.LogError("ItemSpec = " + projectReference.ItemSpec);

                foreach (string metadataName in projectReference.MetadataNames)
                {
                    Log.LogError("MetadataName = " + metadataName);
                    Log.LogError("MetadataValue = " + projectReference.GetMetadata(metadataName));
                }
            }

            return true;

            ////if (string.IsNullOrWhiteSpace(Include))
            ////{
            ////    Log.LogError($"{nameof(Include)} is not set");
            ////    return false;
            ////}

            ////if (!NuGetVersion.TryParse(Version, out var nuGetVersion))
            ////{
            ////    Log.LogError($"{nameof(Version)} is not set or is invalid");
            ////    return false;
            ////}

            ////try
            ////{
            ////    using (var stream = new FileStream(Path.Combine(OutputFile), FileMode.Create, FileAccess.Write))
            ////    {
            ////        var packageIdentity = new PackageIdentity(Include, nuGetVersion);
            ////        var nugetFramework = TargetFramework.ToFramework();
            ////        ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(stream, packageIdentity, nugetFramework).ConfigureAwait(false).GetAwaiter().GetResult();
            ////    }

            ////    return true;
            ////}
            ////catch (Exception ex)
            ////{
            ////    this.Log().Error(ex);
            ////    return false;
            ////}
        }
    }
}
