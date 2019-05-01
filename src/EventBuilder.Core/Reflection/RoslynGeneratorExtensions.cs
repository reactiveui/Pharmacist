// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EventBuilder.Core.Reflection
{
    internal static class RoslynGeneratorExtensions
    {
        /// <summary>
        /// Generates a argument list for a single parameter.
        /// </summary>
        /// <param name="parameter">The parameter to generate the argument list for.</param>
        /// <returns>The argument list.</returns>
        public static ArgumentListSyntax GenerateArgumentList(this IParameter parameter) => ArgumentList(SingletonSeparatedList(Argument(IdentifierName(parameter.Name))));

        /// <summary>
        /// Generates a type argument list for a single type.
        /// </summary>
        /// <param name="type">The type to generate the type argument list for.</param>
        /// <returns>The type argument list.</returns>
        public static TypeArgumentListSyntax GenerateTypeArgumentList(this IType type) => TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(type.GenerateFullGenericName())));

        /// <summary>
        /// Generates a argument list for a tuple parameter.
        /// </summary>
        /// <param name="parameters">The parameters to generate the argument list for.</param>
        /// <returns>The argument list.</returns>
        public static ArgumentListSyntax GenerateTupleArgumentList(this IEnumerable<IParameter> parameters) => ArgumentList(SingletonSeparatedList(Argument(TupleExpression(SeparatedList(parameters.Select(x => Argument(IdentifierName(x.Name))))))));

        /// <summary>
        /// Generates a type argument list for a tuple parameter.
        /// </summary>
        /// <param name="types">The types to generate the argument list for.</param>
        /// <returns>The argument list.</returns>
        public static TypeArgumentListSyntax GenerateTupleTypeArgumentList(this IEnumerable<IType> types)
        {
            return TypeArgumentList(SingletonSeparatedList(types.GenerateTupleType()));
        }

        public static TypeSyntax GenerateTupleType(this IEnumerable<IType> types)
        {
            return TupleType(SeparatedList(types.Select(x => TupleElement(IdentifierName(x.GenerateFullGenericName())))));
        }

        public static TypeArgumentListSyntax GenerateObservableTypeArguments(this IMethod method)
        {
            TypeArgumentListSyntax argumentList;

            // If we have no parameters, use the Unit type, if only one use the type directly, otherwise use a value tuple.
            if (method.Parameters.Count == 0)
            {
                argumentList = TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(RoslynHelpers.ObservableUnitName)));
            }
            else if (method.Parameters.Count == 1)
            {
                argumentList = TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(method.Parameters[0].Type.GenerateFullGenericName())));
            }
            else
            {
                argumentList = TypeArgumentList(SingletonSeparatedList<TypeSyntax>(TupleType(SeparatedList(method.Parameters.Select(x => TupleElement(IdentifierName(x.Type.GenerateFullGenericName())).WithIdentifier(Identifier(x.Name)))))));
            }

            return argumentList;
        }

        public static TypeSyntax GenerateObservableType(this TypeArgumentListSyntax argumentList)
        {
            return QualifiedName(IdentifierName("System"), GenericName(Identifier("IObservable")).WithTypeArgumentList(argumentList));
        }

        public static TypeSyntax GenerateObservableType(this TypeSyntax argumentList)
        {
            return QualifiedName(IdentifierName("System"), GenericName(Identifier("IObservable")).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(argumentList))));
        }

        public static PropertyDeclarationSyntax WithObsoleteAttribute(this PropertyDeclarationSyntax syntax, IEntity eventDetails)
        {
            var attribute = GenerateObsoleteAttributeList(eventDetails);

            if (attribute == null)
            {
                return syntax;
            }

            return syntax.WithAttributeLists(SingletonList(attribute));
        }

        public static ClassDeclarationSyntax WithObsoleteAttribute(this ClassDeclarationSyntax syntax, IEntity eventDetails)
        {
            var attribute = GenerateObsoleteAttributeList(eventDetails);

            if (attribute == null)
            {
                return syntax;
            }

            return syntax.WithAttributeLists(SingletonList(attribute));
        }

        public static MethodDeclarationSyntax WithObsoleteAttribute(this MethodDeclarationSyntax syntax, IEntity eventDetails)
        {
            var attribute = GenerateObsoleteAttributeList(eventDetails);

            if (attribute == null)
            {
                return syntax;
            }

            return syntax.WithAttributeLists(SingletonList(attribute));
        }

        public static ParameterListSyntax GenerateMethodParameters(this IMethod method)
        {
            if (method.Parameters.Count == 0)
            {
                return ParameterList();
            }

            return ParameterList(
                SeparatedList(
                    method.Parameters.Select(
                        x => Parameter(Identifier(x.Name))
                            .WithType(IdentifierName(x.Type.GenerateFullGenericName())))));
        }

        /// <summary>
        /// Gets information about the event's obsolete information if any.
        /// </summary>
        /// <param name="eventDetails">The event details.</param>
        /// <returns>The event's obsolete information if there is any.</returns>
        private static AttributeListSyntax GenerateObsoleteAttributeList(IEntity eventDetails)
        {
            var obsoleteAttribute = eventDetails.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeType.FullName.Equals("System.ObsoleteAttribute", StringComparison.InvariantCulture));

            if (obsoleteAttribute == null)
            {
                return null;
            }

            var message = obsoleteAttribute.FixedArguments.FirstOrDefault().Value.ToString() ?? string.Empty;
            var isError = bool.Parse(obsoleteAttribute.FixedArguments.ElementAtOrDefault(1).Value?.ToString() ?? bool.FalseString) ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;
            var attribute = Attribute(
                IdentifierName("System.ObsoleteAttribute"),
                AttributeArgumentList(SeparatedList(new[] { AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(message))), AttributeArgument(LiteralExpression(isError)) })));

            return AttributeList(SingletonSeparatedList(attribute));
        }
    }
}
