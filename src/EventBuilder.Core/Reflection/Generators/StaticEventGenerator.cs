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

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EventBuilder.Core.Reflection.Generators
{
    internal class StaticEventGenerator : EventGeneratorBase
    {
        /// <summary>
        /// Generate our namespace declarations. These will contain our helper classes.
        /// </summary>
        /// <param name="declarations">The declarations to add.</param>
        /// <returns>An array of namespace declarations.</returns>
        public override IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> declarations) => declarations.GroupBy(x => x.typeDefinition.Namespace)
            .Select(x => NamespaceDeclaration(IdentifierName(x.Key)).WithMembers(List<MemberDeclarationSyntax>(GenerateClasses(x.Key, x))));

        private static ClassDeclarationSyntax GenerateStaticClass(string namespaceName, ITypeDefinition typeDefinition, IEnumerable<IEvent> events)
        {
            // Produces:
            // public static class EventExtensions
            //   contents of members above
            return ClassDeclaration("Events")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class that contains extension methods to wrap events contained within static classes within the {0} namespace.", namespaceName))
                .WithMembers(List<MemberDeclarationSyntax>(events.OrderBy(x => x.Name).Select(x => GenerateEventWrapperObservable(x, typeDefinition.GenerateFullGenericName()))));
        }

        private IEnumerable<ClassDeclarationSyntax> GenerateClasses(string namespaceName, IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> declarations)
        {
            return declarations.OrderBy(x => x.typeDefinition.Name).Select(x => GenerateStaticClass(namespaceName, x.typeDefinition, x.events));
        }
    }
}
