// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Pharmacist.Core.Generation.Generators;

namespace Pharmacist.Core.Generation.Resolvers
{
    /// <summary>
    /// A namespace resolver that extracts event information.
    /// </summary>
    internal abstract class EventNamespaceResolverBase : INamespaceResolver
    {
        /// <inheritdoc />
        public IEnumerable<NamespaceDeclarationSyntax> Create(ICompilation compilation)
        {
            var typesAndEvents = GetValidEventDetails(compilation);

            return GetEventGenerator().Generate(typesAndEvents);
        }

        protected static bool IsValidParameters(IEvent eventDetails)
        {
            var invokeMethod = eventDetails.GetEventType().GetDelegateInvokeMethod();

            // Events must have a valid return type.
            if (invokeMethod == null || invokeMethod.ReturnType.FullName != "System.Void")
            {
                return false;
            }

            if (invokeMethod.Parameters.Any(x => x.IsRef))
            {
                return false;
            }

            return true;
        }

        protected abstract IEventGenerator GetEventGenerator();

        protected abstract IEnumerable<(ITypeDefinition typeHostingEvent, ITypeDefinition baseTypeDefinition, IEnumerable<IEvent> events)> GetValidEventDetails(ICompilation compilation);
    }
}
