﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
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

namespace Pharmacist.Core.Generation.Generators
{
    internal class InstanceEventGenerator : EventGeneratorBase
    {
        private const string DataFieldName = "_data";

        public override IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, ITypeDefinition? baseDefinition, IEnumerable<IEvent> events)> values)
        {
            foreach (var groupedDeclarations in values.GroupBy(x => x.typeDefinition.Namespace).OrderBy(x => x.Key))
            {
                var namespaceName = groupedDeclarations.Key;
                var members = new List<ClassDeclarationSyntax>();

                var orderedTypeDeclarations = groupedDeclarations.OrderBy(x => x.typeDefinition.Name).ToList();

                members.Add(GenerateStaticClass(namespaceName, orderedTypeDeclarations.Select(x => x.typeDefinition)));
                members.AddRange(orderedTypeDeclarations.Select(x => GenerateEventWrapperClass(x.typeDefinition, x.baseDefinition, x.events)).Where(x => x != null));

                if (members.Count > 0)
                {
                    yield return NamespaceDeclaration(IdentifierName(namespaceName))
                        .WithMembers(List<MemberDeclarationSyntax>(members));
                }
            }
        }

        private static ClassDeclarationSyntax GenerateStaticClass(string namespaceName, IEnumerable<ITypeDefinition> declarations)
        {
            // Produces:
            // public static class EventExtensions
            // contents of members above
            return ClassDeclaration("EventExtensions")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class that contains extension methods to wrap events for classes contained within the {0} namespace.", namespaceName))
                .WithMembers(List<MemberDeclarationSyntax>(declarations.Select(declaration =>
                    {
                        var eventsClassName = IdentifierName("Rx" + declaration.Name + "Events");
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
                            .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A wrapper class which wraps all the events contained within the {0} class.", declaration.ConvertToDocument()));
                    })));
        }

        private static ConstructorDeclarationSyntax GenerateEventWrapperClassConstructor(ITypeDefinition typeDefinition, bool hasBaseClass)
        {
            const string dataParameterName = "data";
            var constructor = ConstructorDeclaration(
                    Identifier("Rx" + typeDefinition.Name + "Events"))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                    Identifier(dataParameterName))
                                .WithType(
                                    IdentifierName(typeDefinition.GenerateFullGenericName())))))
                .WithBody(Block(SingletonList(
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(DataFieldName), IdentifierName("data"))))))
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("Initializes a new instance of the {0} class.", typeDefinition.ConvertToDocument(), (dataParameterName, "The class that is being wrapped.")));

            if (hasBaseClass)
            {
                constructor = constructor.WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ArgumentList(SingletonSeparatedList(Argument(IdentifierName(dataParameterName))))));
            }

            return constructor;
        }

        private static FieldDeclarationSyntax GenerateEventWrapperField(ITypeDefinition typeDefinition)
        {
            return FieldDeclaration(VariableDeclaration(IdentifierName(typeDefinition.GenerateFullGenericName()))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(DataFieldName)))))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)));
        }

        private static ClassDeclarationSyntax GenerateEventWrapperClass(ITypeDefinition typeDefinition, ITypeDefinition baseTypeDefinition, IEnumerable<IEvent> events)
        {
            var members = new List<MemberDeclarationSyntax> { GenerateEventWrapperField(typeDefinition), GenerateEventWrapperClassConstructor(typeDefinition, baseTypeDefinition != null) };
            members.AddRange(events.OrderBy(x => x.Name).Select(x => GenerateEventWrapperObservable(x, DataFieldName)).Where(x => x != null).Select(x => x!));

            var classDeclaration = ClassDeclaration("Rx" + typeDefinition.Name + "Events")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithMembers(List(members))
                .WithObsoleteAttribute(typeDefinition)
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class which wraps the events contained within the {0} class as observables.", typeDefinition.ConvertToDocument()));

            if (baseTypeDefinition != null)
            {
                classDeclaration = classDeclaration.WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName($"global::{baseTypeDefinition.Namespace}.{baseTypeDefinition.Name}Events")))));
            }

            return classDeclaration;
        }
    }
}
