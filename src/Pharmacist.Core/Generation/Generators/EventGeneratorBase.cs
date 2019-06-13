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

namespace Pharmacist.Core.Generation.Generators
{
    /// <summary>
    /// Generates common code generation between both static and instance based observables for events.
    /// </summary>
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
        /// <param name="prefix">A prefix to append to the name.</param>
        /// <returns>The property declaration.</returns>
        protected static PropertyDeclarationSyntax GenerateEventWrapperObservable(IEvent eventDetails, string dataObjectName, string prefix = null)
        {
            prefix = prefix ?? string.Empty;

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
                observableEventArgType = SyntaxFactory.IdentifierName(RoslynHelpers.ObservableUnitName).GenerateObservableType();
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
                ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                : SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            return SyntaxFactory.PropertyDeclaration(observableEventArgType, prefix + eventDetails.Name)
                .WithModifiers(modifiers)
                .WithExpressionBody(expressionBody)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithObsoleteAttribute(eventDetails)
                .WithLeadingTrivia(XmlSyntaxFactory.GenerateSummarySeeAlsoComment("Gets an observable which signals when the {0} event triggers.", eventDetails.FullName));
        }

        private static (ArrowExpressionClauseSyntax, TypeSyntax) GenerateFromEventExpression(IEvent eventDetails, IMethod invokeMethod, string dataObjectName)
        {
            var returnType = SyntaxFactory.IdentifierName(eventDetails.ReturnType.GenerateFullGenericName());

            ArgumentListSyntax methodParametersArgumentList;
            TypeSyntax eventArgsType;

            // If we have any members call our observables with the parameters.
            if (invokeMethod.Parameters.Count > 0)
            {
                // If we have only one member, just pass that directly, since our observable will have one generic type parameter.
                // If we have more than one parameter we have to pass them by value tuples, since observables only have one generic type parameter.
                methodParametersArgumentList = invokeMethod.Parameters.Count == 1 ? invokeMethod.Parameters[0].GenerateArgumentList() : invokeMethod.Parameters.GenerateTupleArgumentList();
                eventArgsType = invokeMethod.Parameters.Count == 1 ? SyntaxFactory.IdentifierName(invokeMethod.Parameters[0].Type.GenerateFullGenericName()) : invokeMethod.Parameters.Select(x => x.Type).GenerateTupleType();
            }
            else
            {
                methodParametersArgumentList = RoslynHelpers.ReactiveUnitArgumentList;
                eventArgsType = SyntaxFactory.IdentifierName(RoslynHelpers.ObservableUnitName);
            }

            var eventName = eventDetails.Name;

            var localFunctionExpression = SyntaxFactory.LocalFunctionStatement(
                                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                                SyntaxFactory.Identifier("Handler"))
                                            .WithParameterList(invokeMethod.GenerateMethodParameters())
                                            .WithExpressionBody(
                                                SyntaxFactory.ArrowExpressionClause(
                                                    SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("eventHandler"))
                                                        .WithArgumentList(methodParametersArgumentList)))
                                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var conversionLambdaExpression = SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("eventHandler")),
                SyntaxFactory.Block(localFunctionExpression, SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("Handler"))));

            var fromEventTypeParameters = SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { returnType, SyntaxFactory.Token(SyntaxKind.CommaToken), eventArgsType }));

            var expression = SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("global::System.Reactive.Linq.Observable"),
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("FromEvent"))
                            .WithTypeArgumentList(fromEventTypeParameters)))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[]
                                    {
                                            SyntaxFactory.Argument(conversionLambdaExpression),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            GenerateArgumentEventAccessor(SyntaxKind.AddAssignmentExpression, eventName, dataObjectName),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            GenerateArgumentEventAccessor(SyntaxKind.SubtractAssignmentExpression, eventName, dataObjectName)
                                    }))));

            return (expression, eventArgsType.GenerateObservableType());
        }

        private static ArrowExpressionClauseSyntax GenerateUnitFromEventExpression(IEvent eventDetails, string dataObjectName)
        {
            // This produces "=> global::System.Reactive.Linq.Observable.FromEvent(x => dataObjectName.EventName += x, x => dataObjectName.EventName -= x)".
            // It uses the observable Unit form of the method.
            var eventName = eventDetails.Name;
            return SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("global::System.Reactive.Linq.Observable"),
                            SyntaxFactory.IdentifierName("FromEvent")))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                        new SyntaxNodeOrToken[]
                                        {
                                            GenerateArgumentEventAccessor(SyntaxKind.AddAssignmentExpression, eventName, dataObjectName),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            GenerateArgumentEventAccessor(SyntaxKind.SubtractAssignmentExpression, eventName, dataObjectName)
                                        }))));
        }

        private static (ArrowExpressionClauseSyntax, TypeSyntax) GenerateFromEventPatternExpressionClauseAndType(IEvent eventDetails, string dataObjectName, IMethod invokeMethod)
        {
            // This produces "=> global::System.Reactive.Linq.Observable.FromEventPattern<TEventType<TEventArgs>, TEventArgs>(x => dataObject.EventName += x; x => dataObject.EventName -= x)".
            var param = invokeMethod.Parameters[1];

            var eventArgsName = param.Type.GenerateFullGenericName();
            var eventName = eventDetails.Name;
            var eventArgsType = SyntaxFactory.IdentifierName(eventArgsName);
            var observableEventArgType = eventArgsType.GenerateObservableType();

            var returnType = SyntaxFactory.IdentifierName(eventDetails.ReturnType.GenerateFullGenericName());

            var expression = SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.Reactive.Linq.Observable"),
                                    SyntaxFactory.GenericName(SyntaxFactory.Identifier("FromEventPattern"))
                                        .WithTypeArgumentList(
                                            SyntaxFactory.TypeArgumentList(
                                                SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { returnType, SyntaxFactory.Token(SyntaxKind.CommaToken), eventArgsType })))))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[] { GenerateArgumentEventAccessor(SyntaxKind.AddAssignmentExpression, eventName, dataObjectName), SyntaxFactory.Token(SyntaxKind.CommaToken), GenerateArgumentEventAccessor(SyntaxKind.SubtractAssignmentExpression, eventName, dataObjectName) }))),
                            SyntaxFactory.IdentifierName("Select")))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.SimpleLambdaExpression(
                                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName("x"),
                                                        SyntaxFactory.IdentifierName("EventArgs"))))))));

            return (expression, observableEventArgType);
        }

        private static ArgumentSyntax GenerateArgumentEventAccessor(SyntaxKind accessor, string eventName, string dataObjectName)
        {
            // This produces "x => dataObject.EventName += x" and also "x => dataObject.EventName -= x" depending on the accessor passed in.
            return SyntaxFactory.Argument(
                SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                    SyntaxFactory.AssignmentExpression(
                        accessor,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(dataObjectName),
                            SyntaxFactory.IdentifierName(eventName)),
                        SyntaxFactory.IdentifierName("x"))));
        }
    }
}
