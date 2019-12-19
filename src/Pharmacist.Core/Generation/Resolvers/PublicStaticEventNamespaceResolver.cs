// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
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
        private static readonly ISet<string> SkipNamespaceList = new HashSet<string>(
            new[]
            {
                "ReactiveUI.Events",

                // Winforms
                "System.CodeDom",

                // Xamarin
                "Xamarin.Forms.Xaml.Diagnostics"
            },
            StringComparer.InvariantCulture);

        protected override IEventGenerator GetEventGenerator()
        {
            return new StaticEventGenerator();
        }

        /// <inheritdoc />
        protected override IEnumerable<(ITypeDefinition typeHostingEvent, ITypeDefinition? baseTypeDefinition, IEnumerable<IEvent> events)> GetValidEventDetails(ICompilation compilation)
        {
            var output = new ConcurrentBag<(ITypeDefinition typeHostingEvent, ITypeDefinition? baseTypeDefinition, IEnumerable<IEvent> events)>();

            Parallel.ForEach(
                GetPublicTypesWithEvents(compilation).Where(x => !SkipNamespaceList.Contains(x.Namespace)),
                typeDefinition =>
                {
                    var validEvents = new HashSet<IEvent>(EventNameComparer.Default);

                    foreach (var currentEvent in typeDefinition.Events)
                    {
                        if (!IsValidEvent(currentEvent))
                        {
                            continue;
                        }

                        validEvents.Add(currentEvent);
                    }

                    if (validEvents.Count == 0)
                    {
                        return;
                    }

                    output.Add((typeDefinition, null, validEvents));
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
