// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EventBuilder.Core.Reflection.Generators
{
    internal abstract class EventGeneratorBase : IEventGenerator
    {
        /// <summary>
        /// Generate our namespace declarations. These will contain our helper classes.
        /// </summary>
        /// <param name="declarations">The declarations to add.</param>
        /// <returns>An array of namespace declarations.</returns>
        public abstract IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> declarations);

        /// <summary>
        /// Generates an observable declaration that wraps a event.
        /// </summary>
        /// <param name="eventDetails">The details of the event to wrap.</param>
        /// <param name="dataObjectName">The name of the item where the event is stored.</param>
        /// <returns>The property declaration.</returns>
        protected static PropertyDeclarationSyntax GenerateEventWrapperObservable(IEvent eventDetails, string dataObjectName)
        {
            // Produces:
            // public System.IObservable<eventArgs, eventHandler> EventName => System.Reactive.Linq.Observable.FromEventPattern();
            var eventArgsName = eventDetails.GetEventArgsName();

            if (eventArgsName == null)
            {
                return null;
            }

            return GenerateFromEventPatternAccessor(eventDetails, dataObjectName, eventArgsName);
        }

        private static PropertyDeclarationSyntax GenerateFromEventPatternAccessor(IEvent eventDetails, string dataObjectName, string eventArgsName)
        {
            var eventArgsType = IdentifierName(eventArgsName);
            var observableEventArgType = TypeArgumentList(SingletonSeparatedList<TypeSyntax>(eventArgsType)).GenerateObservableType();

            var returnType = eventDetails.ReturnType.GenerateFullGenericName();

            SyntaxTokenList modifiers = eventDetails.IsStatic
                ? TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                : TokenList(Token(SyntaxKind.PublicKeyword));

            return PropertyDeclaration(observableEventArgType, eventDetails.Name)
                .WithModifiers(modifiers)
                    .WithExpressionBody(
                        ArrowExpressionClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("System.Reactive.Linq.Observable"),
                                            GenericName(Identifier("FromEventPattern"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SeparatedList<TypeSyntax>(
                                                        new SyntaxNodeOrToken[]
                                                        {
                                                            IdentifierName(returnType),
                                                            Token(SyntaxKind.CommaToken),
                                                            eventArgsType
                                                        })))))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SeparatedList<ArgumentSyntax>(
                                                new SyntaxNodeOrToken[]
                                                {
                                                    GenerateArgumentEventAccessor(SyntaxKind.AddAssignmentExpression, eventDetails.Name, dataObjectName),
                                                    Token(SyntaxKind.CommaToken),
                                                    GenerateArgumentEventAccessor(SyntaxKind.SubtractAssignmentExpression, eventDetails.Name, dataObjectName)
                                                }))),
                                    IdentifierName("Select")))
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            SimpleLambdaExpression(
                                                Parameter(
                                                    Identifier("x")),
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("x"),
                                                    IdentifierName("EventArgs")))))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithObsoleteAttribute(eventDetails)
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("Gets an observable which signals when when the {0} event triggers.", eventDetails.FullName));
        }

        private static ArgumentSyntax GenerateArgumentEventAccessor(SyntaxKind accessor, string eventName, string dataObjectName)
        {
            return Argument(
                SimpleLambdaExpression(
                    Parameter(
                        Identifier("x")),
                    AssignmentExpression(
                        accessor,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(dataObjectName),
                            IdentifierName(eventName)),
                        IdentifierName("x"))));
        }
    }
}
