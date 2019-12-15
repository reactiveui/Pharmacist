// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using Pharmacist.Core.Generation;
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
        /// <param name="platforms">The platforms to generate for.</param>
        /// <param name="packageOutputFolder">Directory for the packages, if null a random path in the temp folder will be used.</param>
        /// <returns>A task to monitor the progress.</returns>
        [SuppressMessage("Design", "CA2000: Dispose Object", Justification = "Analyzer can't handle loop based using statements.")]
        public static async Task ExtractEventsFromPlatforms(string outputPath, string prefix, string suffix, string defaultReferenceAssemblyLocation, IEnumerable<AutoPlatform> platforms, string? packageOutputFolder = null)
        {
            var platformExtractors = new IPlatformExtractor[]
                {
                new Android(),
                new iOS(),
                new Mac(),
                new TVOS(),
                new UWP(),
                new Winforms(packageOutputFolder),
                new WPF(packageOutputFolder),
                }.ToDictionary(x => x.Platform);

            if (platforms == null)
            {
                throw new ArgumentNullException(nameof(platforms));
            }

            foreach (var platform in platforms)
            {
                LogHost.Default.Info(CultureInfo.InvariantCulture, "Processing platform {0}", platform);
                var platformExtractor = platformExtractors[platform];
                await platformExtractor.Extract(defaultReferenceAssemblyLocation).ConfigureAwait(false);

                if (platformExtractor.Input == null)
                {
                    throw new Exception("Cannot find valid input from the specified Platform.");
                }

                using (var streamWriter = new StreamWriter(Path.Combine(outputPath, prefix + platform.ToString().ToLowerInvariant() + suffix)))
                {
                    await WriteHeader(streamWriter, platform).ConfigureAwait(false);
                    await ExtractEventsFromAssemblies(streamWriter, platformExtractor.Input, platformExtractor.Framework).ConfigureAwait(false);
                }

                LogHost.Default.Info(CultureInfo.InvariantCulture, "Finished platform {0}", platform);
            }
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
        public static async Task WriteHeader(TextWriter writer, string targetFramework)
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
        /// Writes the header for a output.
        /// </summary>
        /// <param name="writer">The writer where to output to.</param>
        /// <param name="autoPlatform">The packages we are writing for..</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(TextWriter writer, AutoPlatform autoPlatform)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            await WriteHeader(writer).ConfigureAwait(false);

            await writer.WriteLineAsync($"// Platform included: {autoPlatform}").ConfigureAwait(false);

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

            using (var compilation = new EventBuilderCompiler(input, framework))
            {
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
            }
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
