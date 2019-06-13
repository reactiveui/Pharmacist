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

namespace Pharmacist.Core.Generation
{
    internal static class RoslynGeneratorExtensions
    {
        /// <summary>
        /// Generates a argument list for a single parameter.
        /// </summary>
        /// <param name="parameter">The parameter to generate the argument list for.</param>
        /// <returns>The argument list.</returns>
        public static ArgumentListSyntax GenerateArgumentList(this IParameter parameter) => SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Name.GetKeywordSafeName()))));

        /// <summary>
        /// Generates a argument list for a tuple parameter.
        /// </summary>
        /// <param name="parameters">The parameters to generate the argument list for.</param>
        /// <returns>The argument list.</returns>
        public static ArgumentListSyntax GenerateTupleArgumentList(this IEnumerable<IParameter> parameters) => SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(parameters.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name.GetKeywordSafeName()))))))));

        public static TypeSyntax GenerateTupleType(this IEnumerable<IType> types)
        {
            return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(types.Select(x => SyntaxFactory.TupleElement(SyntaxFactory.IdentifierName(x.GenerateFullGenericName())))));
        }

        public static TypeArgumentListSyntax GenerateObservableTypeArguments(this IMethod method)
        {
            TypeArgumentListSyntax argumentList;

            // If we have no parameters, use the Unit type, if only one use the type directly, otherwise use a value tuple.
            if (method.Parameters.Count == 0)
            {
                argumentList = SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName(RoslynHelpers.ObservableUnitName)));
            }
            else if (method.Parameters.Count == 1)
            {
                argumentList = SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName(method.Parameters[0].Type.GenerateFullGenericName())));
            }
            else
            {
                argumentList = SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(method.Parameters.Select(x => SyntaxFactory.TupleElement(SyntaxFactory.IdentifierName(x.Type.GenerateFullGenericName())).WithIdentifier(SyntaxFactory.Identifier(x.Name)))))));
            }

            return argumentList;
        }

        public static TypeSyntax GenerateObservableType(this TypeArgumentListSyntax argumentList)
        {
            return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("global::System"), SyntaxFactory.GenericName(SyntaxFactory.Identifier("IObservable")).WithTypeArgumentList(argumentList));
        }

        public static TypeSyntax GenerateObservableType(this TypeSyntax argumentList)
        {
            return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("global::System"), SyntaxFactory.GenericName(SyntaxFactory.Identifier("IObservable")).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(argumentList))));
        }

        public static PropertyDeclarationSyntax WithObsoleteAttribute(this PropertyDeclarationSyntax syntax, IEntity eventDetails)
        {
            var attribute = GenerateObsoleteAttributeList(eventDetails);

            if (attribute == null)
            {
                return syntax;
            }

            return syntax.WithAttributeLists(SyntaxFactory.SingletonList(attribute));
        }

        public static ClassDeclarationSyntax WithObsoleteAttribute(this ClassDeclarationSyntax syntax, IEntity eventDetails)
        {
            var attribute = GenerateObsoleteAttributeList(eventDetails);

            if (attribute == null)
            {
                return syntax;
            }

            return syntax.WithAttributeLists(SyntaxFactory.SingletonList(attribute));
        }

        public static MethodDeclarationSyntax WithObsoleteAttribute(this MethodDeclarationSyntax syntax, IEntity eventDetails)
        {
            var attribute = GenerateObsoleteAttributeList(eventDetails);

            if (attribute == null)
            {
                return syntax;
            }

            return syntax.WithAttributeLists(SyntaxFactory.SingletonList(attribute));
        }

        public static ParameterListSyntax GenerateMethodParameters(this IMethod method)
        {
            if (method.Parameters.Count == 0)
            {
                return SyntaxFactory.ParameterList();
            }

            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    method.Parameters.Select(
                        x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name))
                            .WithType(SyntaxFactory.IdentifierName(x.Type.GenerateFullGenericName())))));
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
            var attribute = SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("global::System.ObsoleteAttribute"),
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(message))), SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(isError)) })));

            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
        }

        private static string GetKeywordSafeName(this string name)
        {
            return TypesMetadata.CSharpKeywords.Contains(name) ? '@' + name : name;
        }
    }
}
