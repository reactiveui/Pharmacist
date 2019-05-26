// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Console.CommandOptions;
using Pharmacist.Core;
using Pharmacist.Core.NuGet;
using Pharmacist.Core.ReferenceLocators;

using Splat;

using Parser = CommandLine.Parser;

namespace Pharmacist.Console
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // allow app to be debugged in visual studio.
            if (args.Length == 0 && Debugger.IsAttached)
            {
                args = "generate-platform --platforms=uwp --output-path=test.txt".Split(' ');
            }

            var funcLogManager = new FuncLogManager(type => new WrappingFullLogger(new WrappingPrefixLogger(new ConsoleLogger(), type)));
            Locator.CurrentMutable.RegisterConstant(funcLogManager, typeof(ILogManager));

            var parserResult = new Parser(parserSettings => parserSettings.CaseInsensitiveEnumValues = true)
                .ParseArguments<CustomAssembliesCommandLineOptions, PlatformCommandLineOptions>(args);

            var result = await parserResult.MapResult(
                async (PlatformCommandLineOptions options) =>
                {
                    try
                    {
                        string referenceAssembliesLocation;
                        if (!string.IsNullOrWhiteSpace(options.ReferenceAssemblies))
                        {
                            referenceAssembliesLocation = options.ReferenceAssemblies;
                        }
                        else
                        {
                            referenceAssembliesLocation = await ReferenceLocator.GetReferenceLocation().ConfigureAwait(false);
                        }

                        await ObservablesForEventGenerator.ExtractEventsFromPlatforms(options.OutputPath, options.OutputPrefix, referenceAssembliesLocation, options.Platforms).ConfigureAwait(false);

                        return ExitCode.Success;
                    }
                    catch (Exception ex)
                    {
                        LogHost.Default.Fatal(ex);
                        return ExitCode.Error;
                    }
                },
                async (CustomAssembliesCommandLineOptions options) =>
                {
                    try
                    {
                        using (var stream = new FileStream(Path.Combine(options.OutputPath, options.OutputPrefix + ".cs"), FileMode.Create, FileAccess.Write))
                        {
                            await ObservablesForEventGenerator.WriteHeader(stream).ConfigureAwait(false);
                            await ObservablesForEventGenerator.ExtractEventsFromAssemblies(stream, options.Assemblies, options.SearchDirectories).ConfigureAwait(false);
                        }

                        return ExitCode.Success;
                    }
                    catch (Exception ex)
                    {
                        LogHost.Default.Fatal(ex);
                        return ExitCode.Error;
                    }
                },
                async (NugetCommandLineOptions options) =>
                {
                    try
                    {
                        using (var stream = new FileStream(Path.Combine(options.OutputPath, options.OutputPrefix + ".cs"), FileMode.Create, FileAccess.Write))
                        {
                            var packageIdentity = new PackageIdentity(options.NugetPackageName, new NuGetVersion(options.NugetVersion));
                            var nugetFramework = options.TargetFramework.ToFrameworks();
                            await ObservablesForEventGenerator.WriteHeader(stream).ConfigureAwait(false);
                            await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(stream, packageIdentity, nugetFramework).ConfigureAwait(false);
                        }

                        return ExitCode.Success;
                    }
                    catch (Exception ex)
                    {
                        LogHost.Default.Fatal(ex);
                        return ExitCode.Error;
                    }
                },
                _ =>
                {
                    System.Console.WriteLine(HelpText.AutoBuild(parserResult));
                    return Task.FromResult(ExitCode.Error);
                }).ConfigureAwait(false);

            return (int)result;
        }
    }
}
