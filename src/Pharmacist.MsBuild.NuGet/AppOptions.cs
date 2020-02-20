// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CommandLine;

namespace Pharmacist.MsBuild.NuGet
{
    /// <summary>
    /// Options for the application.
    /// </summary>
    public class AppOptions
    {
        /// <summary>
        /// Gets or sets the project references.
        /// </summary>
        [Option('p', "packages", Required = true, HelpText = "The packages to process.", Separator = ';')]
        public List<string> PackageReferences { get; set; }

        /// <summary>
        /// Gets or sets the guids of the project types.
        /// </summary>
        [Option("project-type-guids", HelpText = "The project type guids in the application for old CSPROJ format.")]
        public string ProjectTypeGuids { get; set; }

        /// <summary>
        /// Gets or sets the version of the project type.
        /// </summary>
        [Option("target-framework-version", HelpText = "The version of the target framework for old CSPROJ format.")]
        public string TargetFrameworkVersion { get; set; }

        /// <summary>
        /// Gets or sets the version of the project type associated with UWP projects.
        /// </summary>
        [Option("target-platform-version", HelpText = "The version of the target platform for old CSPROJ format.")]
        public string TargetPlatformVersion { get; set; }

        /// <summary>
        /// Gets or sets the target framework.
        /// </summary>
        [Option("target-framework", HelpText = "The target framework in new CSPROJ format.")]
        public string TargetFramework { get; set; }

        /// <summary>
        /// Gets or sets the output file.
        /// </summary>
        [Option('o', "output", Required = true, HelpText = "The location of the output file.")]
        public string OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the project asset file where the dependencies are stored.
        /// </summary>
        [Option("project-asset-file", HelpText = "The location of the JSON project asset file.")]
        public string ProjectAssetFile { get; set; }
    }
}
