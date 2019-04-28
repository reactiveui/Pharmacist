// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using EventBuilder.Core.Reflection.Compilation;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventBuilder.Core.Reflection
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

        public static ICompilation GetCompilation(IEnumerable<string> targetAssemblies, IEnumerable<string> searchDirectories)
        {
            var modules = targetAssemblies.Select(x => new PEFile(x, PEStreamOptions.PrefetchMetadata));

            return new EventBuilderCompiler(modules, searchDirectories);
        }
    }
}
