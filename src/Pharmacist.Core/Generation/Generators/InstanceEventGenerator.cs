// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
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

        public override IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition TypeDefinition, ITypeDefinition? BaseDefinition, IEnumerable<IEvent> Events)> values)
        {
            foreach (var groupedDeclarations in values.GroupBy(x => x.TypeDefinition.Namespace).OrderBy(x => x.Key))
            {
                var namespaceName = groupedDeclarations.Key;
                var members = new List<ClassDeclarationSyntax>();

                var orderedTypeDeclarations = groupedDeclarations.OrderBy(x => x.TypeDefinition.Name).ToList();

                members.Add(GenerateStaticClass(namespaceName, orderedTypeDeclarations.Select(x => x.TypeDefinition)));
                members.AddRange(orderedTypeDeclarations.Select(x => GenerateEventWrapperClass(x.TypeDefinition, x.BaseDefinition, x.Events)).Where(x => x != null));

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
                        return BuildMethodDeclaration(declaration)
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                            .WithParameterList(ParameterList(SingletonSeparatedList(
                                Parameter(Identifier("item"))
                                    .WithModifiers(TokenList(Token(SyntaxKind.ThisKeyword)))
                                    .WithType(IdentifierName(declaration.GenerateFullGenericName())))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            .WithObsoleteAttribute(declaration)
                            .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A wrapper class which wraps all the events contained within the {0} class.", declaration.ConvertToDocument()));

                        static MethodDeclarationSyntax BuildMethodDeclaration(ITypeDefinition declaration)
                        {
                            if (declaration.IsUnboundGenericTypeDefinition())
                            {
                                var args = string.Join(", ", declaration.TypeArguments.Select(param => param.FullName));
                                var genericEventsClassName = IdentifierName("Rx" + declaration.Name + "Events<" + args + ">");
                                return MethodDeclaration(genericEventsClassName, Identifier("Events"))
                                    .WithTypeParameterList(TypeParameterList(
                                        Token(SyntaxKind.LessThanToken),
                                        SeparatedList(declaration.TypeArguments.Select(arg => TypeParameter(arg.FullName))),
                                        Token(SyntaxKind.GreaterThanToken)))
                                    .WithExpressionBody(ArrowExpressionClause(
                                        ObjectCreationExpression(genericEventsClassName)
                                            .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                                Argument(IdentifierName("item")))))));
                            }

                            var eventsClassName = IdentifierName("Rx" + declaration.Name + "Events");
                            return MethodDeclaration(eventsClassName, Identifier("Events"))
                                .WithExpressionBody(ArrowExpressionClause(
                                    ObjectCreationExpression(eventsClassName)
                                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                                            Argument(IdentifierName("item")))))));
                        }
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

        private static ClassDeclarationSyntax GenerateEventWrapperClass(ITypeDefinition typeDefinition, ITypeDefinition? baseTypeDefinition, IEnumerable<IEvent> events)
        {
            var members = new List<MemberDeclarationSyntax> { GenerateEventWrapperField(typeDefinition), GenerateEventWrapperClassConstructor(typeDefinition, baseTypeDefinition != null) };
            members.AddRange(events.OrderBy(x => x.Name).Select(x => GenerateEventWrapperObservable(x, DataFieldName)).Where(x => x != null).Select(x => x!));

            var classDeclaration = ClassDeclaration("Rx" + typeDefinition.Name + "Events")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithMembers(List(members))
                .WithObsoleteAttribute(typeDefinition)
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("A class which wraps the events contained within the {0} class as observables.", typeDefinition.ConvertToDocument()));

            if (typeDefinition.IsUnboundGenericTypeDefinition())
            {
                classDeclaration = classDeclaration.WithTypeParameterList(TypeParameterList(
                    Token(SyntaxKind.LessThanToken),
                    SeparatedList(typeDefinition.TypeArguments.Select(arg => TypeParameter(arg.FullName))),
                    Token(SyntaxKind.GreaterThanToken)));
            }

            if (baseTypeDefinition != null)
            {
                var baseTypeName = $"global::{baseTypeDefinition.Namespace}.Rx{baseTypeDefinition.Name}Events";
                if (baseTypeDefinition.IsUnboundGenericTypeDefinition())
                {
                    var directBaseType = typeDefinition.DirectBaseTypes
                        .First(directBase => directBase.FullName == baseTypeDefinition.FullName);
                    var argumentList = directBaseType.TypeArguments.Select(arg => arg.GenerateFullGenericName());
                    var argumentString = "<" + string.Join(", ", argumentList) + ">";
                    baseTypeName += argumentString;
                }

                classDeclaration = classDeclaration.WithBaseList(BaseList(
                    SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(
                        IdentifierName(baseTypeName)))));
            }

            return classDeclaration;
        }
    }
}
