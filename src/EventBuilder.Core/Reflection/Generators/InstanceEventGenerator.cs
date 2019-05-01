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
    internal class InstanceEventGenerator : EventGeneratorBase
    {
        private const string DataFieldName = "_data";

        public override IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> declarations) => declarations.GroupBy(x => x.typeDefinition.Namespace)
            .Select(x => GenerateNamespace(x.Key, x));

        private static ClassDeclarationSyntax GenerateStaticClass(string namespaceName, IEnumerable<ITypeDefinition> declarations)
        {
            // Produces:
            // public static class EventExtensions
            //   contents of members above
            return ClassDeclaration("EventExtensions")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class that contains extension methods to wrap events for classes contained within the {0} namespace.", namespaceName))
                .WithMembers(List<MemberDeclarationSyntax>(declarations.Select(declaration =>
                    {
                        var eventsClassName = IdentifierName(declaration.Name + "Events");
                        return MethodDeclaration(eventsClassName, Identifier("Events"))
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                            .WithParameterList(ParameterList(SingletonSeparatedList(
                                Parameter(Identifier("item"))
                                    .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                                    .WithType(IdentifierName(declaration.GenerateFullGenericName())))))
                            .WithExpressionBody(ArrowExpressionClause(
                                ObjectCreationExpression(eventsClassName)
                                    .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName("item")))))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            .WithObsoleteAttribute(declaration)
                            .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A wrapper class which wraps all the events contained within the {0} class.", declaration.GenerateFullGenericName()));
                    })));
        }

        private static ConstructorDeclarationSyntax GenerateEventWrapperClassConstructor(ITypeDefinition typeDefinition)
        {
            return ConstructorDeclaration(
                    Identifier(typeDefinition.Name + "Events"))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                    Identifier("data"))
                                .WithType(
                                    IdentifierName(typeDefinition.GenerateFullGenericName())))))
                .WithBody(Block(SingletonList(
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(DataFieldName), IdentifierName("data"))))))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("Initializes a new instance of the {0} class.", typeDefinition.GenerateFullGenericName(), ("data", "The class that is being wrapped.")));
        }

        private static FieldDeclarationSyntax GenerateEventWrapperField(ITypeDefinition typeDefinition)
        {
            return FieldDeclaration(VariableDeclaration(IdentifierName(typeDefinition.GenerateFullGenericName()))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(DataFieldName)))))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)));
        }

        private static ClassDeclarationSyntax GenerateEventWrapperClass(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)
        {
            var members = new List<MemberDeclarationSyntax> { GenerateEventWrapperField(typeDefinition), GenerateEventWrapperClassConstructor(typeDefinition) };
            members.AddRange(events.OrderBy(x => x.Name).Select(x => GenerateEventWrapperObservable(x, DataFieldName)).Where(x => x != null));

            return ClassDeclaration(typeDefinition.Name + "Events")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithMembers(List(members))
                .WithObsoleteAttribute(typeDefinition)
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class which wraps the events contained within the {0} class as observables.", typeDefinition.GenerateFullGenericName()));
        }

        private static NamespaceDeclarationSyntax GenerateNamespace(string namespaceName, IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> declarations)
        {
            var members = new List<ClassDeclarationSyntax>();

            var orderedTypeDeclarations = declarations.OrderBy(x => x.typeDefinition.Name).ToList();

            members.Add(GenerateStaticClass(namespaceName, orderedTypeDeclarations.Select(x => x.typeDefinition)));
            members.AddRange(orderedTypeDeclarations.Select(x => GenerateEventWrapperClass(x.typeDefinition, x.events)).Where(x => x != null));

            var namespaceDeclaration = NamespaceDeclaration(IdentifierName(namespaceName));

            if (members.Count > 0)
            {
                return namespaceDeclaration.WithMembers(List<MemberDeclarationSyntax>(members));
            }

            return namespaceDeclaration;
        }
    }
}
