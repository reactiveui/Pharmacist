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
    /// <summary>
    /// Generates code syntax based on the Delegate based methodology
    /// where we derive from a base class and override methods.
    /// We provide an observable in this case.
    /// </summary>
    internal static class DelegateGenerator
    {
        private static readonly QualifiedNameSyntax _subjectNamespace = QualifiedName(IdentifierName("Pharmacist"), IdentifierName("Common"));
        private static readonly GenericNameSyntax _subjectType = GenericName(Identifier("SingleAwaitSubject"));

        /// <summary>
        /// Generate our namespace declarations. These will contain our helper classes.
        /// </summary>
        /// <param name="declarations">The declarations to add.</param>
        /// <returns>An array of namespace declarations.</returns>
        internal static IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, bool isAbstract, IEnumerable<IMethod> methods)> declarations)
        {
            foreach (var groupedDeclarations in declarations.GroupBy(x => x.typeDefinition.Namespace))
            {
                var namespaceName = groupedDeclarations.Key;
                var members = new List<ClassDeclarationSyntax>();

                members.AddRange(groupedDeclarations.OrderBy(x => x.typeDefinition.Name).Select(x => GenerateClass(x.typeDefinition, x.isAbstract, x.methods)));

                if (members.Count > 0)
                {
                    yield return NamespaceDeclaration(IdentifierName(namespaceName))
                        .WithMembers(List<MemberDeclarationSyntax>(members));
                }
            }
        }

        /// <summary>
        /// Generates our helper classes with the observables.
        /// </summary>
        /// <param name="typeDefinition">The type definition containing the information.</param>
        /// <param name="isAbstract">If the delegates are abstract.</param>
        /// <param name="methods">The methods to generate delegate overloads for.</param>
        /// <returns>The generated class declarations.</returns>
        private static ClassDeclarationSyntax GenerateClass(ITypeDefinition typeDefinition, bool isAbstract, IEnumerable<IMethod> methods)
        {
                var modifiers = typeDefinition.IsAbstract || isAbstract
                    ? TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AbstractKeyword), Token(SyntaxKind.PartialKeyword))
                    : TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword));
                return ClassDeclaration(typeDefinition.Name + "Rx")
                    .WithModifiers(modifiers)
                    .WithMembers(List(GenerateObservableMembers(methods)))
                    .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName(typeDefinition.GenerateFullGenericName())))))
                    .WithLeadingTrivia(GenerateSummarySeeAlsoComment("Wraps delegates events from {0} into Observables.", typeDefinition.GenerateFullGenericName()))
                    .WithObsoleteAttribute(typeDefinition);
        }

        private static IEnumerable<MemberDeclarationSyntax> GenerateObservableMembers(IEnumerable<IMethod> methods)
        {
            var methodDeclarations = new List<MethodDeclarationSyntax>();
            var fieldDeclarations = new List<FieldDeclarationSyntax>();
            var propertyDeclarations = new List<PropertyDeclarationSyntax>();

            foreach (var method in methods.OrderBy(y => y.Name))
            {
                var observableName = "_" + char.ToLowerInvariant(method.Name[0]) + method.Name.Substring(1);
                methodDeclarations.Add(GenerateMethodDeclaration(observableName, method));
                fieldDeclarations.Add(GenerateFieldDeclaration(observableName, method));
                propertyDeclarations.Add(GeneratePropertyDeclaration(observableName, method));
            }

            return fieldDeclarations.Cast<MemberDeclarationSyntax>().Concat(propertyDeclarations).Concat(methodDeclarations);
        }

        /// <summary>
        /// Produces the property declaration for the observable.
        /// </summary>
        /// <param name="observableName">The field name of the observable.</param>
        /// <param name="method">The method we are abstracting.</param>
        /// <returns>The property declaration.</returns>
        private static PropertyDeclarationSyntax GeneratePropertyDeclaration(string observableName, IMethod method)
        {
            // Produces:
            // public System.IObservable<type> MethodNameObs => _observableName;
            return PropertyDeclaration(method.GenerateObservableTypeArguments().GenerateObservableType(), Identifier(method.Name + "Obs"))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithObsoleteAttribute(method)
                .WithExpressionBody(ArrowExpressionClause(IdentifierName(observableName)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithLeadingTrivia(GenerateSummarySeeAlsoComment("Gets an observable which signals when the {0} method is invoked.", method.FullName));
        }

        /// <summary>
        /// Produces the field declaration which contains the subject.
        /// </summary>
        /// <param name="observableName">The field name of the observable.</param>
        /// <param name="method">The method we are abstracting.</param>
        /// <returns>The field declaration.</returns>
        private static FieldDeclarationSyntax GenerateFieldDeclaration(string observableName, IMethod method)
        {
            // Produces:
            // private readonly ReactiveUI.Events.SingleAwaitSubject<type> _methodName = new ReactiveUI.Events.SingleAwaitSubject<type>();
            var typeName = QualifiedName(_subjectNamespace, _subjectType.WithTypeArgumentList(method.GenerateObservableTypeArguments()));

            return FieldDeclaration(VariableDeclaration(typeName)
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier(observableName))
                            .WithInitializer(
                                EqualsValueClause(
                                    ObjectCreationExpression(typeName).WithArgumentList(ArgumentList()))))))
                .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)));
        }

        private static MethodDeclarationSyntax GenerateMethodDeclaration(string observableName, IMethod method)
        {
            // Produces:
            // /// <inheritdoc />
            // public override void MethodName(params..) => _methodName.OnNext(...);
            var methodBody = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(observableName), IdentifierName("OnNext")));

            var methodParameterList = method.GenerateMethodParameters();

            // If we have any members call our observables with the parameters.
            if (method.Parameters.Count > 0)
            {
                // If we have only one member, just pass that directly, since our observable will have one generic type parameter.
                // If we have more than one parameter we have to pass them by value tuples, since observables only have one generic type parameter.
                if (method.Parameters.Count == 1)
                {
                    methodBody = methodBody.WithArgumentList(method.Parameters[0].GenerateArgumentList());
                }
                else
                {
                    methodBody = methodBody.WithArgumentList(method.Parameters.GenerateTupleArgumentList());
                }
            }
            else
            {
                methodBody = methodBody.WithArgumentList(RoslynHelpers.ReactiveUnitArgumentList);
            }

            return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), method.Name)
                .WithExpressionBody(ArrowExpressionClause(methodBody))
                .WithParameterList(methodParameterList)
                .WithObsoleteAttribute(method)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithLeadingTrivia(InheritdocSyntax)
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }
    }
}
