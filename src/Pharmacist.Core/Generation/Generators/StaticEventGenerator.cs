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
using static Pharmacist.Core.Generation.XmlSyntaxFactory;

namespace Pharmacist.Core.Generation.Generators
{
    internal class StaticEventGenerator : EventGeneratorBase
    {
        /// <summary>
        /// Generate our namespace declarations. These will contain our helper classes.
        /// </summary>
        /// <param name="declarations">The declarations to add.</param>
        /// <returns>An array of namespace declarations.</returns>
        public override IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition TypeDefinition, ITypeDefinition? BaseDefinition, IEnumerable<IEvent> Events)> declarations)
        {
            foreach (var groupDeclaration in declarations.GroupBy(x => x.TypeDefinition.Namespace).OrderBy(x => x.Key))
            {
                var namespaceName = groupDeclaration.Key;

                var eventWrapperMembers = groupDeclaration
                    .OrderBy(x => x.TypeDefinition.Name)
                    .SelectMany(
                        x =>
                            x.Events
                                .OrderBy(eventDetails => eventDetails.Name)
                                .Select(eventDetails => GenerateEventWrapperObservable(eventDetails, x.TypeDefinition.GenerateFullGenericName(), x.TypeDefinition.Name))
                                .Where(y => y != null))
                    .ToList();

                if (eventWrapperMembers.Count > 0)
                {
                    var members = ClassDeclaration("Events")
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                        .WithLeadingTrivia(GenerateSummarySeeAlsoComment("A class that contains extension methods to wrap events contained within static classes within the {0} namespace.", namespaceName))
                        .WithMembers(List<MemberDeclarationSyntax>(eventWrapperMembers.Where(x => x != null).Select(x => x!)));

                    yield return NamespaceDeclaration(IdentifierName(namespaceName))
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(members));
                }
            }
        }
    }
}
