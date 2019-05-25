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
    internal class InstanceEventGenerator : EventGeneratorBase
    {
        private const string DataFieldName = "_data";

        public override IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> declarations)
        {
            foreach (var groupedDeclarations in declarations.GroupBy(x => x.typeDefinition.Namespace))
            {
                var namespaceName = groupedDeclarations.Key;
                var members = new List<ClassDeclarationSyntax>();

                var orderedTypeDeclarations = groupedDeclarations.OrderBy(x => x.typeDefinition.Name).ToList();

                members.Add(GenerateStaticClass(namespaceName, orderedTypeDeclarations.Select(x => x.typeDefinition)));
                members.AddRange(orderedTypeDeclarations.Select(x => GenerateEventWrapperClass(x.typeDefinition, x.events)).Where(x => x != null));

                if (members.Count > 0)
                {
                    yield return SyntaxFactory
                        .NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
                        .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(members));
                }
            }
        }

        private static ClassDeclarationSyntax GenerateStaticClass(string namespaceName, IEnumerable<ITypeDefinition> declarations)
        {
            // Produces:
            // public static class EventExtensions
            //   contents of members above
            return SyntaxFactory.ClassDeclaration("EventExtensions")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class that contains extension methods to wrap events for classes contained within the {0} namespace.", namespaceName))
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(declarations.Select(declaration =>
                    {
                        var eventsClassName = SyntaxFactory.IdentifierName(declaration.Name + "Events");
                        return SyntaxFactory.MethodDeclaration(eventsClassName, SyntaxFactory.Identifier("Events"))
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("item"))
                                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)))
                                    .WithType(SyntaxFactory.IdentifierName(declaration.GenerateFullGenericName())))))
                            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                                SyntaxFactory.ObjectCreationExpression(eventsClassName)
                                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("item")))))))
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            .WithObsoleteAttribute(declaration)
                            .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A wrapper class which wraps all the events contained within the {0} class.", declaration.GenerateFullGenericName()));
                    })));
        }

        private static ConstructorDeclarationSyntax GenerateEventWrapperClassConstructor(ITypeDefinition typeDefinition)
        {
            return SyntaxFactory.ConstructorDeclaration(
                    SyntaxFactory.Identifier(typeDefinition.Name + "Events"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Parameter(
                                    SyntaxFactory.Identifier("data"))
                                .WithType(
                                    SyntaxFactory.IdentifierName(typeDefinition.GenerateFullGenericName())))))
                .WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(DataFieldName), SyntaxFactory.IdentifierName("data"))))))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("Initializes a new instance of the {0} class.", typeDefinition.GenerateFullGenericName(), ("data", "The class that is being wrapped.")));
        }

        private static FieldDeclarationSyntax GenerateEventWrapperField(ITypeDefinition typeDefinition)
        {
            return SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(typeDefinition.GenerateFullGenericName()))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(DataFieldName)))))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
        }

        private static ClassDeclarationSyntax GenerateEventWrapperClass(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)
        {
            var members = new List<MemberDeclarationSyntax> { GenerateEventWrapperField(typeDefinition), GenerateEventWrapperClassConstructor(typeDefinition) };
            members.AddRange(events.OrderBy(x => x.Name).Select(x => GenerateEventWrapperObservable(x, DataFieldName)).Where(x => x != null));

            return SyntaxFactory.ClassDeclaration(typeDefinition.Name + "Events")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithMembers(SyntaxFactory.List(members))
                .WithObsoleteAttribute(typeDefinition)
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class which wraps the events contained within the {0} class as observables.", typeDefinition.GenerateFullGenericName()));
        }
    }
}
