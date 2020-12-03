﻿// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Type parameter of a generic class/method.
	/// </summary>
	public interface ITypeParameter : IType, ISymbol
	{
		/// <summary>
		/// Get the type of this type parameter's owner.
		/// </summary>
		/// <returns>SymbolKind.TypeDefinition or SymbolKind.Method</returns>
		SymbolKind OwnerType { get; }

		/// <summary>
		/// Gets the owning method/class.
		/// This property may return null (for example for the dummy type parameters used by <see cref="NormalizeTypeVisitor.ReplaceMethodTypeParametersWithDummy"/>).
		/// </summary>
		/// <remarks>
		/// For "class Outer&lt;T&gt; { class Inner {} }",
		/// inner.TypeParameters[0].Owner will be the outer class, because the same
		/// ITypeParameter instance is used both on Outer`1 and Outer`1+Inner.
		/// </remarks>
		IEntity Owner { get; }

		/// <summary>
		/// Gets the index of the type parameter in the type parameter list of the owning method/class.
		/// </summary>
		int Index { get; }

		/// <summary>
		/// Gets the name of the type parameter.
		/// </summary>
		new string Name { get; }

		/// <summary>
		/// Gets the attributes declared on this type parameter.
		/// </summary>
		IEnumerable<IAttribute> GetAttributes();

		/// <summary>
		/// Gets the variance of this type parameter.
		/// </summary>
		VarianceModifier Variance { get; }

		/// <summary>
		/// Gets the effective base class of this type parameter.
		/// </summary>
		IType EffectiveBaseClass { get; }

		/// <summary>
		/// Gets the effective interface set of this type parameter.
		/// </summary>
		IReadOnlyCollection<IType> EffectiveInterfaceSet { get; }

		/// <summary>
		/// Gets if the type parameter has the 'new()' constraint.
		/// </summary>
		bool HasDefaultConstructorConstraint { get; }

		/// <summary>
		/// Gets if the type parameter has the 'class' constraint.
		/// </summary>
		bool HasReferenceTypeConstraint { get; }

		/// <summary>
		/// Gets if the type parameter has the 'struct' or 'unmanaged' constraint.
		/// </summary>
		bool HasValueTypeConstraint { get; }

		/// <summary>
		/// Gets if the type parameter has the 'unmanaged' constraint.
		/// </summary>
		bool HasUnmanagedConstraint { get; }

		/// <summary>
		/// Nullability of the reference type constraint. (e.g. "where T : class?").
		/// 
		/// Note that the nullability of a use of the type parameter may differ from this.
		/// E.g. "T? GetNull&lt;T&gt;() where T : class => null;"
		/// </summary>
		Nullability NullabilityConstraint { get; }

		IReadOnlyList<TypeConstraint> TypeConstraints { get; }
	}

	public readonly struct TypeConstraint
	{
		public SymbolKind SymbolKind => SymbolKind.Constraint;
		public IType Type { get; }
		public IReadOnlyList<IAttribute> Attributes { get; }

		public TypeConstraint(IType type, IReadOnlyList<IAttribute> attributes = null)
		{
			this.Type = type ?? throw new ArgumentNullException(nameof(type));
			this.Attributes = attributes ?? EmptyList<IAttribute>.Instance;
		}
	}

	/// <summary>
	/// Represents the variance of a type parameter.
	/// </summary>
	public enum VarianceModifier : byte
	{
		/// <summary>
		/// The type parameter is not variant.
		/// </summary>
		Invariant,
		/// <summary>
		/// The type parameter is covariant (used in output position).
		/// </summary>
		Covariant,
		/// <summary>
		/// The type parameter is contravariant (used in input position).
		/// </summary>
		Contravariant
	};
}
