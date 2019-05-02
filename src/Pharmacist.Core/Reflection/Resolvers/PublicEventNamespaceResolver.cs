// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

using Pharmacist.Core.Reflection.Generators;

namespace Pharmacist.Core.Reflection.Resolvers
{
    /// <summary>
    /// A namespace resolver that extracts event information.
    /// </summary>
    internal class PublicEventNamespaceResolver : EventNamespaceResolverBase
    {
        protected override IEnumerable<IEvent> GetValidEventDetails(IEnumerable<IEvent> eventDetails)
        {
            return eventDetails.Where(x => x.Accessibility == Accessibility.Public && !x.IsStatic);
        }

        protected override IEnumerable<ITypeDefinition> GetPublicTypesWithEvents(ICompilation compilation)
        {
            return compilation.GetPublicTypesWithNotStaticEvents();
        }

        protected override IEventGenerator GetEventGenerator()
        {
            return new InstanceEventGenerator();
        }
    }
}
