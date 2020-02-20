// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CommandLine;
using Serilog;

namespace Pharmacist.MsBuild.NuGet
{
    /// <summary>
    /// Class that hosts the main entry point to the application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main execution point of the application.
        /// </summary>
        /// <param name="parameters">The parameters passed to the application.</param>
        /// <returns>The return value of the application.</returns>
        public static int Main(string[] parameters)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            var result = Parser.Default.ParseArguments<AppOptions>(parameters).MapResult(PharmacistNuGetRunner.Execute, _ => 1);
            return result;
        }
    }
}
