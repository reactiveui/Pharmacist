// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pharmacist.Core.Generation.Generators
{
    internal class StaticEventGenerator : EventGeneratorBase
    {
        /// <summary>
        /// Generate our namespace declarations. These will contain our helper classes.
        /// </summary>
        /// <param name="declarations">The declarations to add.</param>
        /// <returns>An array of namespace declarations.</returns>
        public override IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> declarations)
        {
            foreach (var groupDeclaration in declarations.GroupBy(x => x.typeDefinition.Namespace))
            {
                var namespaceName = groupDeclaration.Key;

                var members = groupDeclaration.OrderBy(x => x.typeDefinition.Name).Select(x => GenerateStaticClass(namespaceName, x.typeDefinition, x.events)).Where(x => x != null).ToList();

                if (members.Any())
                {
                    yield return SyntaxFactory
                        .NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
                        .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(members));
                }
            }
        }

        private static ClassDeclarationSyntax GenerateStaticClass(string namespaceName, ITypeDefinition typeDefinition, IEnumerable<IEvent> events)
        {
            var members = events.OrderBy(x => x.Name).Select(x => GenerateEventWrapperObservable(x, typeDefinition.GenerateFullGenericName())).Where(x => x != null).ToList();

            if (members.Count > 0)
            {
                return null;
            }

            // Produces:
            // public static class EventExtensions
            //   contents of members above
            return SyntaxFactory.ClassDeclaration("Events")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class that contains extension methods to wrap events contained within static classes within the {0} namespace.", namespaceName))
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(members));
        }
    }
}
