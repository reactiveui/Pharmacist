// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EventBuilder.Core.Extractors;
using EventBuilder.Core.Extractors.PlatformExtractors;
using EventBuilder.Core.Reflection;
using EventBuilder.Core.Reflection.Resolvers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NuGet.Frameworks;
using NuGet.Packaging.Core;

using Serilog;

namespace EventBuilder.Core
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
                Log.Information("Processing platform {0}", platform);
                var platformExtractor = _platformExtractors[platform];
                await platformExtractor.Extract(defaultReferenceAssemblyLocation).ConfigureAwait(false);

                using (var stream = new FileStream(Path.Combine(outputPath, prefix + ".cs"), FileMode.Create, FileAccess.Write))
                {
                    await ExtractEventsFromAssemblies(stream, platformExtractor.Assemblies, platformExtractor.SearchDirectories).ConfigureAwait(false);
                }

                Log.Information("Finished platform {0}", platform);
            }
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputStream">Stream that we should output to.</param>
        /// <param name="package">The package to process.</param>
        /// <param name="framework">The framework to generate for.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromNuGetPackages(Stream outputStream, PackageIdentity package, NuGetFramework framework)
        {
            Log.Information("Processing NuGet package {0}", package);

            var extractor = new NuGetExtractor();
            await extractor.Extract(framework, package).ConfigureAwait(false);

            await ExtractEventsFromAssemblies(outputStream, extractor.Assemblies, extractor.SearchDirectories).ConfigureAwait(false);

            Log.Information("Finished NuGet package {0}", package);
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
            var compilation = RoslynHelpers.GetCompilation(assemblyPaths, searchDirectories);

            var compilationOutputSyntax = SyntaxFactory.CompilationUnit().WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(_resolvers.SelectMany(x => x.Create(compilation))));

            StreamWriter streamWriter = new StreamWriter(outputStream);
            await streamWriter.WriteAsync(await TemplateManager.GetTemplateAsync(TemplateManager.HeaderTemplate).ConfigureAwait(false)).ConfigureAwait(false);
            await streamWriter.WriteAsync(Environment.NewLine).ConfigureAwait(false);
            await streamWriter.WriteAsync(compilationOutputSyntax.NormalizeWhitespace(elasticTrivia: true).ToString()).ConfigureAwait(false);
            await streamWriter.FlushAsync().ConfigureAwait(false);
        }
    }
}
