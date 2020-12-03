// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

using CommandLine;

using Pharmacist.Core;

namespace Pharmacist.Console.CommandOptions
{
    /// <summary>
    /// Command line options for the platform based generation.
    /// </summary>
    [Verb("generate-platform", HelpText = "Generate from a predetermined platform.")]
    public class PlatformCommandLineOptions : CommandLineOptionsBase
    {
        /// <summary>
        /// Gets or sets the target framework.
        /// </summary>
        [Option('t', "target-frameworks", Required = true, HelpText = "Specify the target framework monikiers.", Separator = ',')]
        public IEnumerable<string>? TargetFrameworks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use WPF.
        /// </summary>
        [Option("is-wpf", Required = false, HelpText = "Specify if WPF libraries should be used.")]
        public bool IsWpf { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use WinForms.
        /// </summary>
        [Option("is-winforms", Required = false, HelpText = "Specify if WinForms libraries should be used.")]
        public bool IsWinForms { get; set; }

        /// <summary>
        /// Gets or sets the reference assemblies.
        /// </summary>
        [Option('r', "reference", Required = false, HelpText = "Specify a Reference Assemblies location to override the default.")]
        public string? ReferenceAssemblies { get; set; }
    }
}
