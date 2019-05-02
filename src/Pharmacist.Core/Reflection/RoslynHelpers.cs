// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Pharmacist.Core.Reflection.Compilation;

namespace Pharmacist.Core.Reflection
{
    /// <summary>
    /// Helper methods associated with the roslyn template generators.
    /// </summary>
    internal static class RoslynHelpers
    {
        internal const string ObservableUnitName = "System.Reactive.Unit";
        internal const string VoidType = "System.Void";

        /// <summary>
        /// Gets an argument which access System.Reactive.Unit.Default member.
        /// </summary>
        public static ArgumentListSyntax ReactiveUnitArgumentList { get; } = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(ObservableUnitName + ".Default"))));

        /// <summary>
        /// Gets an type argument which access System.Reactive.Unit.Default member.
        /// </summary>
        public static TypeArgumentListSyntax ReactiveUnitTypeArgumentList { get; } = SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName(ObservableUnitName)));

        public static ICompilation GetCompilation(IEnumerable<string> targetAssemblies, IEnumerable<string> searchDirectories)
        {
            var modules = targetAssemblies.Select(x => new PEFile(x, PEStreamOptions.PrefetchMetadata));

            var foundDirectories = new HashSet<string>();
            foreach (var searchDirectory in searchDirectories)
            {
                var directoryInfo = new DirectoryInfo(searchDirectory);
                foundDirectories.UnionWith(
                    directoryInfo.EnumerateDirectories("*.*", SearchOption.AllDirectories)
                        .Where(x => x.EnumerateFiles().Any(file => file.FullName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) || file.FullName.EndsWith(".winmd", StringComparison.InvariantCultureIgnoreCase)))
                        .Select(x => x.FullName));
            }

            return new EventBuilderCompiler(modules, foundDirectories.ToList());
        }
    }
}
