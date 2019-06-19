// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.TypeSystem;

using Pharmacist.Core.Generation.Generators;

namespace Pharmacist.Core.Generation.Resolvers
{
    internal class PublicStaticEventNamespaceResolver : EventNamespaceResolverBase
    {
        protected override IEventGenerator GetEventGenerator()
        {
            return new StaticEventGenerator();
        }

        /// <inheritdoc />
        protected override IEnumerable<(ITypeDefinition typeHostingEvent, ITypeDefinition baseTypeDefinition, IEnumerable<IEvent> events)> GetValidEventDetails(ICompilation compilation)
        {
            var output = new ConcurrentBag<(ITypeDefinition typeHostingEvent, ITypeDefinition baseTypeDefinition, IEnumerable<IEvent> events)>();

            Parallel.ForEach(
                GetPublicTypesWithEvents(compilation),
                typeDefinition =>
                {
                    var events = typeDefinition.Events.Where(IsValidEvent).ToList();

                    if (events.Count == 0)
                    {
                        return;
                    }

                    output.Add((typeDefinition, null, events));
                });

            return output;
        }

        private static IEnumerable<ITypeDefinition> GetPublicTypesWithEvents(ICompilation compilation)
        {
            return compilation.GetPublicTypesWithStaticEvents();
        }

        private static bool IsValidEvent(IEvent eventDetails) => eventDetails.Accessibility == Accessibility.Public && eventDetails.IsStatic && IsValidParameters(eventDetails);
    }
}
