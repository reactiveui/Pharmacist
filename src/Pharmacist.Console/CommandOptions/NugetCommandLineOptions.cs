// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CommandLine;

namespace Pharmacist.Console.CommandOptions
{
    /// <summary>
    /// Command line options for working in a NuGet package mode.
    /// </summary>
    [Verb("generate-nuget", HelpText = "Generate from a specified NuGet package.")]
    public class NugetCommandLineOptions : CommandLineOptionsBase
    {
        /// <summary>
        /// Gets or sets the platform.
        /// </summary>
        [Option('p', "package", Required = true, HelpText = "The name of the NuGet package.")]
        public string NugetPackageName { get; set; }

        /// <summary>
        /// Gets or sets the reference assemblies.
        /// </summary>
        [Option('v', "version", Required = true, HelpText = "Specify the NuGet version number.")]
        public string NugetVersion { get; set; }

        /// <summary>
        /// Gets or sets the reference assemblies.
        /// </summary>
        [Option('t', "target-framework", Required = true, HelpText = "Specify the Target framework to extract for.")]
        public string TargetFramework { get; set; }
    }
}
