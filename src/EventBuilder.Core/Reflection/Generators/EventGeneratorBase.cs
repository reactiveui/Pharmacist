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
            var invokeMethod = eventDetails.GetEventType().GetDelegateInvokeMethod();

            ArrowExpressionClauseSyntax expressionBody;
            TypeSyntax observableEventArgType;

            // Events must have a valid return type.
            if (invokeMethod == null || invokeMethod.ReturnType.FullName != "System.Void")
            {
                return null;
            }

            // If we are using a standard approach of using 2 parameters use the "FromEventPattern", otherwise, use "FromEvent" where we have to use converters.
            if (invokeMethod.Parameters.Count == 2 && invokeMethod.Parameters[0].Type.FullName == "System.Object")
            {
                (expressionBody, observableEventArgType) = GenerateFromEventPatternExpressionClauseAndType(eventDetails, dataObjectName, invokeMethod);
            }
            else if (invokeMethod.Parameters.Count == 0)
            {
                observableEventArgType = IdentifierName(RoslynHelpers.ObservableUnitName).GenerateObservableType();
                expressionBody = GenerateUnitFromEventExpression(eventDetails, dataObjectName);
            }
            else
            {
                (expressionBody, observableEventArgType) = GenerateFromEventExpression(eventDetails, invokeMethod, dataObjectName);
            }

            if (observableEventArgType == null || expressionBody == null)
            {
                return null;
            }

            SyntaxTokenList modifiers = eventDetails.IsStatic
                ? TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                : TokenList(Token(SyntaxKind.PublicKeyword));

            return PropertyDeclaration(observableEventArgType, eventDetails.Name)
                .WithModifiers(modifiers)
                .WithExpressionBody(expressionBody)
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithObsoleteAttribute(eventDetails)
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("Gets an observable which signals when when the {0} event triggers.", eventDetails.FullName));
        }

        private static (ArrowExpressionClauseSyntax, TypeSyntax) GenerateFromEventExpression(IEvent eventDetails, IMethod invokeMethod, string dataObjectName)
        {
            var returnType = IdentifierName(eventDetails.ReturnType.GenerateFullGenericName());

            ArgumentListSyntax methodParametersArgumentList;
            TypeSyntax eventArgsType;

            // If we have any members call our observables with the parameters.
            if (invokeMethod.Parameters.Count > 0)
            {
                // If we have only one member, just pass that directly, since our observable will have one generic type parameter.
                // If we have more than one parameter we have to pass them by value tuples, since observables only have one generic type parameter.
                methodParametersArgumentList = invokeMethod.Parameters.Count == 1 ? invokeMethod.Parameters[0].GenerateArgumentList() : invokeMethod.Parameters.GenerateTupleArgumentList();
                eventArgsType = invokeMethod.Parameters.Count == 1 ? IdentifierName(invokeMethod.Parameters[0].Type.GenerateFullGenericName()) : invokeMethod.Parameters.Select(x => x.Type).GenerateTupleType();
            }
            else
            {
                methodParametersArgumentList = RoslynHelpers.ReactiveUnitArgumentList;
                eventArgsType = IdentifierName(RoslynHelpers.ObservableUnitName);
            }

            var eventName = eventDetails.Name;

            var localFunctionExpression = LocalFunctionStatement(
                                                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                                                Identifier("Handler"))
                                            .WithParameterList(invokeMethod.GenerateMethodParameters())
                                            .WithExpressionBody(
                                                ArrowExpressionClause(
                                                    InvocationExpression(IdentifierName("eventHandler"))
                                                        .WithArgumentList(methodParametersArgumentList)))
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            var conversionLambdaExpression = SimpleLambdaExpression(
                Parameter(Identifier("eventHandler")),
                Block(localFunctionExpression, ReturnStatement(IdentifierName("Handler"))));

            var fromEventTypeParameters = TypeArgumentList(SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { returnType, Token(SyntaxKind.CommaToken), eventArgsType }));

            var expression = ArrowExpressionClause(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("System.Reactive.Linq.Observable"),
                        GenericName(Identifier("FromEvent"))
                            .WithTypeArgumentList(fromEventTypeParameters)))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[]
                                    {
                                            Argument(conversionLambdaExpression),
                                            Token(SyntaxKind.CommaToken),
                                            GenerateArgumentEventAccessor(SyntaxKind.AddAssignmentExpression, eventName, dataObjectName),
                                            Token(SyntaxKind.CommaToken),
                                            GenerateArgumentEventAccessor(SyntaxKind.SubtractAssignmentExpression, eventName, dataObjectName)
                                    }))));

            return (expression, eventArgsType.GenerateObservableType());
        }

        private static ArrowExpressionClauseSyntax GenerateUnitFromEventExpression(IEvent eventDetails, string dataObjectName)
        {
            var eventName = eventDetails.Name;
            return ArrowExpressionClause(
                InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("System.Reactive.Linq.Observable"),
                            IdentifierName("FromEvent")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList<ArgumentSyntax>(
                                        new SyntaxNodeOrToken[]
                                        {
                                            GenerateArgumentEventAccessor(SyntaxKind.AddAssignmentExpression, eventName, dataObjectName),
                                            Token(SyntaxKind.CommaToken),
                                            GenerateArgumentEventAccessor(SyntaxKind.SubtractAssignmentExpression, eventName, dataObjectName)
                                        }))));
        }

        private static (ArrowExpressionClauseSyntax, TypeSyntax) GenerateFromEventPatternExpressionClauseAndType(IEvent eventDetails, string dataObjectName, IMethod invokeMethod)
        {
            var param = invokeMethod.Parameters[1];

            var eventArgsName = param.Type.GenerateFullGenericName();
            var eventName = eventDetails.Name;
            var eventArgsType = IdentifierName(eventArgsName);
            var observableEventArgType = eventArgsType.GenerateObservableType();

            var returnType = IdentifierName(eventDetails.ReturnType.GenerateFullGenericName());

            var expression = ArrowExpressionClause(
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
                                                SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { returnType, Token(SyntaxKind.CommaToken), eventArgsType })))))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[] { GenerateArgumentEventAccessor(SyntaxKind.AddAssignmentExpression, eventName, dataObjectName), Token(SyntaxKind.CommaToken), GenerateArgumentEventAccessor(SyntaxKind.SubtractAssignmentExpression, eventName, dataObjectName) }))),
                            IdentifierName("Select")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                SimpleLambdaExpression(
                                                    Parameter(Identifier("x")),
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("x"),
                                                        IdentifierName("EventArgs"))))))));

            return (expression, observableEventArgType);
        }

        private static ArgumentSyntax GenerateArgumentEventAccessor(SyntaxKind accessor, string eventName, string dataObjectName)
        {
            return Argument(
                SimpleLambdaExpression(
                    Parameter(Identifier("x")),
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
