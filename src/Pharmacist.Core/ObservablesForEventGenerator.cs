// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;

using Pharmacist.Core.Extractors;
using Pharmacist.Core.Extractors.PlatformExtractors;
using Pharmacist.Core.Generation.Compilation;
using Pharmacist.Core.Generation.Resolvers;
using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;
using Pharmacist.Core.Utilities;

using Splat;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pharmacist.Core
{
    /// <summary>
    /// Processes the specified platform and saves out a specified template.
    /// </summary>
    public static class ObservablesForEventGenerator
    {
        private static readonly INamespaceResolver[] _resolvers =
        {
            new PublicEventNamespaceResolver(),
            new PublicStaticEventNamespaceResolver(),
            new DelegateTemplateNamespaceResolver()
        };

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputPath">The path where to output the files.</param>
        /// <param name="prefix">The prefix to add to the start of the output file.</param>
        /// <param name="suffix">The suffix to add to the end of output file names.</param>
        /// <param name="defaultReferenceAssemblyLocation">A directory path to where reference assemblies can be located.</param>
        /// <param name="targetFramework">The target platform to generate for.</param>
        /// <param name="isWpf">If to generate for WPF.</param>
        /// <param name="isWinForms">If to generate for WinForms.</param>
        /// <param name="packageOutputFolder">Directory for the packages, if null a random path in the temp folder will be used.</param>
        /// <param name="includeHeader">If the header should be included.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromPlatforms(string outputPath, string prefix, string suffix, string defaultReferenceAssemblyLocation, string targetFramework, bool isWpf, bool isWinForms, string? packageOutputFolder = null, bool includeHeader = true)
        {
            var platformExtractors = new List<IPlatformExtractor>
                                     {
                                         new Android(),
                                         new iOS(),
                                         new Mac(),
                                         new TVOS(),
                                         new UWP(),
                                         new WatchOs()
                                     };

            if (isWinForms)
            {
                platformExtractors.Add(new Winforms(packageOutputFolder));
            }

            if (isWpf)
            {
                platformExtractors.Add(new WPF(packageOutputFolder));
            }

            LogHost.Default.Info("Starting to process " + targetFramework);

            var frameworks = targetFramework.ToFrameworks().ToArray();

            if (frameworks.Length == 0)
            {
                LogHost.Default.Error(CultureInfo.InvariantCulture, "Could not find any valid frameworks for {0}", targetFramework);
                return;
            }

            var framework = frameworks[0];

            var extractors = platformExtractors.Where(x => x.CanExtract(frameworks)).ToList();

            if (extractors.Count == 0)
            {
                throw new InvalidOperationException(
                    "Could not find valid extractors for framework " + framework);
            }

            var inputGroup = new InputAssembliesGroup();
            foreach (var extractor in extractors)
            {
                await extractor.Extract(frameworks, defaultReferenceAssemblyLocation).ConfigureAwait(false);

                if (extractor.Input == null)
                {
                    throw new Exception("Cannot find valid input from the specified Platform.");
                }

                inputGroup = inputGroup.Combine(extractor.Input);
            }

            using var streamWriter = new StreamWriter(Path.Combine(outputPath, prefix + targetFramework.ToLowerInvariant() + suffix));

            if (includeHeader)
            {
                await WriteHeader(streamWriter, framework).ConfigureAwait(false);
            }

            await ExtractEventsFromAssemblies(streamWriter, inputGroup, framework).ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="packages">The packages to process.</param>
        /// <param name="frameworks">The framework to generate for in order of priority.</param>
        /// <param name="packageOutputFolder">Directory for the packages, if null a random path in the temp folder will be used.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromNuGetPackages(TextWriter writer, IReadOnlyCollection<PackageIdentity> packages, IReadOnlyCollection<NuGetFramework> frameworks, string? packageOutputFolder = null)
        {
            var extractor = new NuGetExtractor();
            await extractor.Extract(frameworks, packages, packageOutputFolder).ConfigureAwait(false);

            if (extractor.Input == null)
            {
                throw new Exception("Cannot find valid input from the specified NuGet package.");
            }

            await ExtractEventsFromAssemblies(writer, extractor.Input, frameworks.First()).ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="packages">The packages to process.</param>
        /// <param name="frameworks">The framework to generate for in order of priority.</param>
        /// <param name="packageOutputFolder">Directory for the packages, if null a random path in the temp folder will be used.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromNuGetPackages(TextWriter writer, IReadOnlyCollection<LibraryRange> packages, IReadOnlyCollection<NuGetFramework> frameworks, string? packageOutputFolder = null)
        {
            var extractor = new NuGetExtractor();
            await extractor.Extract(frameworks, packages, packageOutputFolder).ConfigureAwait(false);

            if (extractor.Input == null)
            {
                throw new Exception("Cannot find valid input from the specified NuGet package.");
            }

            await ExtractEventsFromAssemblies(writer, extractor.Input, frameworks.First()).ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="assemblies">The assemblies to extract.</param>
        /// <param name="searchDirectories">Directories to search.</param>
        /// <param name="targetFramework">The name of the TFM to extract for.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromAssemblies(TextWriter writer, IEnumerable<string> assemblies, IEnumerable<string> searchDirectories, string targetFramework)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var input = new InputAssembliesGroup();
            input.IncludeGroup.AddFiles(assemblies);
            input.SupportGroup.AddFiles(FileSystemHelpers.GetFilesWithinSubdirectories(searchDirectories, AssemblyHelpers.AssemblyFileExtensionsSet));

            var framework = targetFramework.ToFrameworks();

            await ExtractEventsFromAssemblies(writer, input, framework[0]).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the header for a output.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            await writer.WriteAsync(await TemplateManager.GetTemplateAsync(TemplateManager.HeaderTemplate).ConfigureAwait(false)).ConfigureAwait(false);
            await writer.WriteLineAsync("// Generated with Pharmacist version: " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the header for a output.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="targetFramework">The target framework being generated.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(TextWriter writer, NuGetFramework targetFramework)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            await WriteHeader(writer).ConfigureAwait(false);

            await writer.WriteAsync("// Target Framework Included:  " + targetFramework).ConfigureAwait(false);

            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the header for a output.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="libraryRanges">The library ranges to include as packages included in the output.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(TextWriter writer, IReadOnlyCollection<LibraryRange> libraryRanges)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (libraryRanges == null)
            {
                throw new ArgumentNullException(nameof(libraryRanges));
            }

            await WriteHeader(writer).ConfigureAwait(false);

            foreach (var libraryRange in libraryRanges)
            {
                await writer.WriteLineAsync($"// Package included: {libraryRange}").ConfigureAwait(false);
            }

            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the header for a output.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="packageIdentities">The packages included in the output.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(TextWriter writer, IReadOnlyCollection<PackageIdentity> packageIdentities)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (packageIdentities == null)
            {
                throw new ArgumentNullException(nameof(packageIdentities));
            }

            await WriteHeader(writer).ConfigureAwait(false);

            foreach (var packageIdentity in packageIdentities)
            {
                await writer.WriteLineAsync($"// Package included: {packageIdentity}").ConfigureAwait(false);
            }

            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the header for a output.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="fileNames">The file name to write.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(TextWriter writer, IEnumerable<string> fileNames)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (fileNames == null)
            {
                throw new ArgumentNullException(nameof(fileNames));
            }

            await WriteHeader(writer).ConfigureAwait(false);

            await writer.WriteLineAsync($"// Assemblies included: {string.Join(", ", fileNames)}").ConfigureAwait(false);

            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="input">The input into the processor.</param>
        /// <param name="framework">The framework we are adapting to.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromAssemblies(TextWriter writer, InputAssembliesGroup input, NuGetFramework framework)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var compilation = new EventBuilderCompiler(input, framework);
            var compilationOutputSyntax = CompilationUnit()
                .WithMembers(List<MemberDeclarationSyntax>(_resolvers.SelectMany(x => x.Create(compilation))))
                .WithUsings(List(new[]
                                 {
                                         UsingDirective(IdentifierName("global::System")),
                                         UsingDirective(IdentifierName("global::System.Reactive")),
                                         UsingDirective(IdentifierName("global::System.Reactive.Linq")),
                                         UsingDirective(IdentifierName("global::System.Reactive.Subjects")),
                                         UsingDirective(IdentifierName("global::Pharmacist.Common"))
                                 }));

            await writer.WriteAsync(Environment.NewLine).ConfigureAwait(false);
            await writer.WriteAsync(compilationOutputSyntax.NormalizeWhitespace(elasticTrivia: true).ToString()).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);

            compilation.Dispose();
        }

        /// <summary>
        /// Extracts the events and delegates from the specified target frameworks.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="frameworks">The frameworks we are adapting to.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromTargetFramework(TextWriter writer, IReadOnlyList<NuGetFramework> frameworks)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            if (frameworks.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameworks));
            }

            var mainFramework = frameworks[0];

            LogHost.Default.Info(CultureInfo.InvariantCulture, "Processing target framework {0}", mainFramework);

            var input = new InputAssembliesGroup();
            input.IncludeGroup.AddFiles(FileSystemHelpers.GetFilesWithinSubdirectories(mainFramework.GetNuGetFrameworkFolders(), AssemblyHelpers.AssemblyFileExtensionsSet));

            await ExtractEventsFromAssemblies(writer, input, mainFramework).ConfigureAwait(false);

            LogHost.Default.Info(CultureInfo.InvariantCulture, "Finished target framework {0}", mainFramework);
        }
    }
}
