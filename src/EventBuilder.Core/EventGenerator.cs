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

using EventBuilder.Core.Extractors.PlatformExtractors;
using EventBuilder.Core.Reflection;
using EventBuilder.Core.Reflection.Resolvers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;

namespace EventBuilder.Core
{
    /// <summary>
    /// Processes the specified platform and saves out a specified template.
    /// </summary>
    public static class EventGenerator
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
            new Essentials(),
            new iOS(),
            new Mac(),
            new Tizen(),
            new TVOS(),
            new UWP(),
            new Winforms(),
            new WPF(),
            new XamForms(),
        }.ToImmutableDictionary(x => x.Platform);

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputPath">The path where to output the files.</param>
        /// <param name="prefix">The prefix to add to the start of the output file.</param>
        /// <param name="defaultReferenceAssemblyLocation">A directory path to where reference assemblies can be located.</param>
        /// <param name="platforms">The platforms to generate for.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromAssemblies(string outputPath, string prefix, string defaultReferenceAssemblyLocation, IEnumerable<AutoPlatform> platforms)
        {
            foreach (var platform in platforms)
            {
                Log.Information("Processing platform {0}", platform);
                var platformExtractor = _platformExtractors[platform];
                await platformExtractor.Extract(defaultReferenceAssemblyLocation).ConfigureAwait(false);

                await ExtractEventsFromAssemblies(outputPath, prefix + platform, platformExtractor.Assemblies, platformExtractor.SearchDirectories).ConfigureAwait(false);
                Log.Information("Finished platform {0}", platform);
            }
        }

        /// <summary>
        /// Extracts the events and delegates from the specified platform.
        /// </summary>
        /// <param name="outputPath">The path where to output the files.</param>
        /// <param name="prefix">The prefix to add to the start of the output file.</param>
        /// <param name="assemblyPaths">The paths to the assemblies to extract.</param>
        /// <param name="searchDirectories">Paths to any directories to search for supporting libraries.</param>
        /// <returns>A task to monitor the progress.</returns>
        public static async Task ExtractEventsFromAssemblies(string outputPath, string prefix, IEnumerable<string> assemblyPaths, IEnumerable<string> searchDirectories)
        {
            var compilation = RoslynHelpers.GetCompilation(assemblyPaths, searchDirectories);

            var compilationOutputSyntax = SyntaxFactory.CompilationUnit().WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(_resolvers.SelectMany(x => x.Create(compilation))));

            using (StreamWriter streamWriter = new StreamWriter(Path.Combine(outputPath, prefix + ".cs")))
            {
                await streamWriter.WriteAsync(await TemplateManager.GetTemplateAsync(TemplateManager.HeaderTemplate).ConfigureAwait(false)).ConfigureAwait(false);
                await streamWriter.WriteAsync(Environment.NewLine).ConfigureAwait(false);
                await streamWriter.WriteAsync(compilationOutputSyntax.NormalizeWhitespace(elasticTrivia: true).ToString()).ConfigureAwait(false);
            }
        }
    }
}
