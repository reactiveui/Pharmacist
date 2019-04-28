// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventBuilder.Core.Reflection.Generators
{
    /// <summary>
    /// Generates based on events in the base code.
    /// </summary>
    internal interface IEventGenerator
    {
        /// <summary>
        /// Generates a compilation unit based on generating event observable wrappers.
        /// </summary>
        /// <param name="values">The values to generate for.</param>
        /// <returns>The new compilation unit.</returns>
        IEnumerable<NamespaceDeclarationSyntax> Generate(IEnumerable<(ITypeDefinition typeDefinition, IEnumerable<IEvent> events)> values);
    }
}
