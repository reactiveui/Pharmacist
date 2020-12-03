// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using NuGet.LibraryModel;
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
        [SuppressMessage("Design", "CA1031: Catch specific exceptions", Justification = "Final logging location for exceptions.")]
        public static async Task<int> Main(string[] args)
        {
            // allow app to be debugged in visual studio.
            if (args.Length == 0 && Debugger.IsAttached)
            {
                args = "generate-platform --is-wpf --target-frameworks=net5.0,net461,net462,net463,net470,net471,net472,net48 --output-path=c:/temp --output-prefix=blah".Split(' ');
            }

            var funcLogManager = new FuncLogManager(type => new WrappingFullLogger(new WrappingPrefixLogger(new ConsoleLogger(), type)));
            Locator.CurrentMutable.RegisterConstant(funcLogManager, typeof(ILogManager));

            using var parser = new Parser(parserSettings => parserSettings.CaseInsensitiveEnumValues = true);
            var parserResult = parser
                .ParseArguments<CustomAssembliesCommandLineOptions, PlatformCommandLineOptions>(args);

            var result = await parserResult.MapResult(
                async (PlatformCommandLineOptions options) =>
                {
                    try
                    {
                        var referenceAssembliesLocation = !string.IsNullOrWhiteSpace(options.ReferenceAssemblies)
                            ? options.ReferenceAssemblies
                            : ReferenceLocator.GetReferenceLocation();

                        if (string.IsNullOrWhiteSpace(options.OutputPath))
                        {
                            throw new Exception("Invalid output path for the event generation.");
                        }

                        if (string.IsNullOrWhiteSpace(options.OutputPrefix))
                        {
                            throw new Exception("Invalid output prefix for the event generation.");
                        }

                        if (options.TargetFrameworks == null)
                        {
                            throw new Exception("Invalid target framework for the event generation.");
                        }

                        foreach (var targetFramework in options.TargetFrameworks)
                        {
                            await ObservablesForEventGenerator.ExtractEventsFromPlatforms(
                                options.OutputPath,
                                options.OutputPrefix,
                                ".cs",
                                referenceAssembliesLocation,
                                targetFramework,
                                options.IsWpf,
                                options.IsWinForms).ConfigureAwait(false);
                        }

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
                        if (options.OutputPath == null)
                        {
                            throw new InvalidOperationException("There is no Output path specified.");
                        }

                        await using var writer = new StreamWriter(Path.Combine(options.OutputPath, options.OutputPrefix + ".cs"));

                        if (options.Assemblies == null)
                        {
                            throw new Exception("Invalid specified assemblies for observable generation.");
                        }

                        if (options.SearchDirectories == null)
                        {
                            throw new Exception("Invalid search directories specified for observable generation.");
                        }

                        if (string.IsNullOrWhiteSpace(options.TargetFramework))
                        {
                            throw new Exception("Invalid target framework for the event generation.");
                        }

                        await ObservablesForEventGenerator.WriteHeader(writer, options.Assemblies).ConfigureAwait(false);

                        await ObservablesForEventGenerator.ExtractEventsFromAssemblies(writer, options.Assemblies, options.SearchDirectories, options.TargetFramework).ConfigureAwait(false);

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
                        if (options.OutputPath == null)
                        {
                            throw new InvalidOperationException("There is no Output path specified.");
                        }

                        await using var writer = new StreamWriter(Path.Combine(options.OutputPath, options.OutputPrefix + ".cs"));
                        if (string.IsNullOrWhiteSpace(options.TargetFramework))
                        {
                            throw new Exception("Invalid target framework for the event generation.");
                        }

                        var packageIdentity = new[] { new LibraryRange(options.NugetPackageName, VersionRange.Parse(options.NugetVersion), LibraryDependencyTarget.Package) };
                        var nugetFramework = options.TargetFramework.ToFrameworks();
                        await ObservablesForEventGenerator.WriteHeader(writer, packageIdentity).ConfigureAwait(false);
                        await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(writer, packageIdentity, nugetFramework, options.PackageFolder).ConfigureAwait(false);

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
