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
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Generation.Resolvers
{
    /// <summary>
    /// A namespace resolver that extracts event information.
    /// </summary>
    internal class PublicEventNamespaceResolver : EventNamespaceResolverBase
    {
        private const string SkipNamespaceName = "ReactiveUI.Events";

        /// <inheritdoc />
        protected override IEnumerable<(ITypeDefinition typeHostingEvent, ITypeDefinition baseTypeDefinition, IEnumerable<IEvent> events)> GetValidEventDetails(ICompilation compilation)
        {
            var processedList = new ConcurrentDictionary<ITypeDefinition, bool>();
            var toProcess = new ConcurrentStack<ITypeDefinition>(GetPublicTypesWithEvents(compilation).Where(x => !x.Namespace.StartsWith(SkipNamespaceName, StringComparison.InvariantCulture)));
            var output = new ConcurrentBag<(ITypeDefinition typeHostingEvent, ITypeDefinition baseTypeDefinition, IEnumerable<IEvent> events)>();

            var processing = new ITypeDefinition[Environment.ProcessorCount];
            while (!toProcess.IsEmpty)
            {
                var count = toProcess.TryPopRange(processing);

                var processingList = processing.Take(count);
                Parallel.ForEach(
                    processingList,
                    typeDefinition =>
                    {
                        if (!processedList.TryAdd(typeDefinition, true))
                        {
                            return;
                        }

                        var validEvents = typeDefinition.Events.Where(IsValidEvent).ToList();

                        if (validEvents.Count == 0)
                        {
                            return;
                        }

                        processedList.TryAdd(typeDefinition, true);

                        var baseType = GetValidBaseType(typeDefinition, compilation);

                        if (baseType != null)
                        {
                            toProcess.Push(baseType);
                        }

                        output.Add((typeDefinition, baseType, validEvents));
                    });
            }

            return output;
        }

        protected override IEventGenerator GetEventGenerator()
        {
            return new InstanceEventGenerator();
        }

        private static ITypeDefinition GetValidBaseType(ITypeDefinition typeDefinition, ICompilation compilation)
        {
            var processedTypes = new HashSet<ITypeDefinition>();
            var processingQueue = new Queue<IType>(typeDefinition.DirectBaseTypes);

            while (processingQueue.Count != 0)
            {
                var currentType = processingQueue.Dequeue().GetRealType(compilation).GetDefinition();

                if (currentType == null || currentType.Kind == TypeKind.Interface || currentType.Kind == TypeKind.TypeParameter)
                {
                    continue;
                }

                if (processedTypes.Contains(currentType))
                {
                    continue;
                }

                processedTypes.Add(currentType);

                if (currentType.Events.Any(IsValidEvent))
                {
                    return currentType;
                }

                processingQueue.EnqueueRange(currentType.DirectBaseTypes);
            }

            return null;
        }

        private static IEnumerable<ITypeDefinition> GetPublicTypesWithEvents(ICompilation compilation)
        {
            return compilation.GetPublicTypesWithNotStaticEvents();
        }

        private static bool IsValidEvent(IEvent x) => x.Accessibility == Accessibility.Public && !x.IsStatic && IsValidParameters(x);
    }
}
