// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CommandLine;

namespace Pharmacist.Console.CommandOptions
{
    /// <summary>
    /// A base class for commonly shared options.
    /// </summary>
    public abstract class CommandLineOptionsBase
    {
        /// <summary>
        /// Gets or sets the path where to output the contents.
        /// </summary>
        [Option('o', "output-path", Required = true, HelpText = "The directory path where to output the contents.")]
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the output file prefix.
        /// </summary>
        [Option("output-prefix", Required = true, HelpText = "Specify a prefix for the output file based on the platforms selected. Each platform output file will contain this prefix.")]
        public string OutputPrefix { get; set; }
    }
}
