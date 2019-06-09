// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;

using Pharmacist.Core.Extractors;
using Pharmacist.Core.Extractors.PlatformExtractors;
using Pharmacist.Core.Generation;
using Pharmacist.Core.Generation.Resolvers;

using Splat;

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

        private static readonly IDictionary<AutoPlatform, IPlatformExtractor> _platformExtractors = new IPlatformExtractor[]
        {
            new Android(),
            new iOS(),
            new Mac(),
            new TVOS(),
            new UWP(),
            new Winforms(),
            new WPF(),
        }.ToImmutableDictionary(x => x.Platform);

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputPath">The path where to output the files.</param>
        /// <param name="prefix">The prefix to add to the start of the output file.</param>
        /// <param name="defaultReferenceAssemblyLocation">A directory path to where reference assemblies can be located.</param>
        /// <param name="platforms">The platforms to generate for.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromPlatforms(string outputPath, string prefix, string defaultReferenceAssemblyLocation, IEnumerable<AutoPlatform> platforms)
        {
            foreach (var platform in platforms)
            {
                LogHost.Default.Info(CultureInfo.InvariantCulture, "Processing platform {0}", platform);
                var platformExtractor = _platformExtractors[platform];
                await platformExtractor.Extract(defaultReferenceAssemblyLocation).ConfigureAwait(false);

                using (var stream = new FileStream(Path.Combine(outputPath, prefix + ".cs"), FileMode.Create, FileAccess.Write))
                {
                    await WriteHeader(stream).ConfigureAwait(false);
                    await ExtractEventsFromAssemblies(stream, platformExtractor.Assemblies, platformExtractor.SearchDirectories).ConfigureAwait(false);
                }

                LogHost.Default.Info(CultureInfo.InvariantCulture, "Finished platform {0}", platform);
            }
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputStream">Stream that we should output to.</param>
        /// <param name="packages">The packages to process.</param>
        /// <param name="frameworks">The framework to generate for in order of priority.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromNuGetPackages(Stream outputStream, IReadOnlyCollection<PackageIdentity> packages, IReadOnlyCollection<NuGetFramework> frameworks)
        {
            var extractor = new NuGetExtractor();
            await extractor.Extract(frameworks, packages).ConfigureAwait(false);

            await ExtractEventsFromAssemblies(outputStream, extractor.Assemblies, extractor.SearchDirectories).ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputStream">Stream that we should output to.</param>
        /// <param name="packages">The packages to process.</param>
        /// <param name="frameworks">The framework to generate for in order of priority.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromNuGetPackages(Stream outputStream, IReadOnlyCollection<LibraryRange> packages, IReadOnlyCollection<NuGetFramework> frameworks)
        {
            var extractor = new NuGetExtractor();
            await extractor.Extract(frameworks, packages).ConfigureAwait(false);

            await ExtractEventsFromAssemblies(outputStream, extractor.Assemblies, extractor.SearchDirectories).ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputStream">Stream that we should output to.</param>
        /// <param name="assemblyPaths">The paths to the assemblies to extract.</param>
        /// <param name="searchDirectories">Paths to any directories to search for supporting libraries.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromAssemblies(Stream outputStream, IEnumerable<string> assemblyPaths, IEnumerable<string> searchDirectories)
        {
            using (var compilation = RoslynHelpers.GetCompilation(assemblyPaths, searchDirectories))
            {
                var compilationOutputSyntax = SyntaxFactory.CompilationUnit().WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(_resolvers.SelectMany(x => x.Create(compilation))));

                StreamWriter streamWriter = new StreamWriter(outputStream);
                await streamWriter.WriteAsync(Environment.NewLine).ConfigureAwait(false);
                await streamWriter.WriteAsync(compilationOutputSyntax.NormalizeWhitespace(elasticTrivia: true).ToString()).ConfigureAwait(false);
                await streamWriter.FlushAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Writes the header for a output.
        /// </summary>
        /// <param name="outputStream">The stream where to write to.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(Stream outputStream)
        {
            StreamWriter streamWriter = new StreamWriter(outputStream);
            await streamWriter.WriteAsync(await TemplateManager.GetTemplateAsync(TemplateManager.HeaderTemplate).ConfigureAwait(false)).ConfigureAwait(false);
            await streamWriter.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes the header for a output.
        /// </summary>
        /// <param name="outputStream">The stream where to write to.</param>
        /// <param name="libraryRanges">The library ranges to include as packages included in the output.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task WriteHeader(Stream outputStream, IReadOnlyCollection<LibraryRange> libraryRanges)
        {
            StreamWriter streamWriter = new StreamWriter(outputStream);
            await streamWriter.WriteAsync(await TemplateManager.GetTemplateAsync(TemplateManager.HeaderTemplate).ConfigureAwait(false)).ConfigureAwait(false);

            foreach (var libraryRange in libraryRanges)
            {
                await streamWriter.WriteLineAsync($"// Package included {libraryRange}").ConfigureAwait(false);
            }

            await streamWriter.FlushAsync().ConfigureAwait(false);
        }
    }
}
