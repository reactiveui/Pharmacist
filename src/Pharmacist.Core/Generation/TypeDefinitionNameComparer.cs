﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler.TypeSystem;

namespace Pharmacist.Core.Generation
{
    /// <summary>
    /// A comparer which will compare <see cref="ITypeDefinition"/> names.
    /// </summary>
    internal class TypeDefinitionNameComparer : IEqualityComparer<ITypeDefinition>, IComparer<ITypeDefinition>
    {
        public static TypeDefinitionNameComparer Default { get; } = new TypeDefinitionNameComparer();

        /// <inheritdoc />
        public bool Equals(ITypeDefinition? x, ITypeDefinition? y)
        {
            return StringComparer.Ordinal.Equals(x?.GenerateFullGenericName(), y?.GenerateFullGenericName());
        }

        /// <inheritdoc />
        public int GetHashCode(ITypeDefinition obj)
        {
            return StringComparer.Ordinal.GetHashCode(obj.GenerateFullGenericName());
        }

        /// <inheritdoc />
        public int Compare(ITypeDefinition? x, ITypeDefinition? y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return string.CompareOrdinal(x.GenerateFullGenericName(), y.GenerateFullGenericName());
        }
    }
}
