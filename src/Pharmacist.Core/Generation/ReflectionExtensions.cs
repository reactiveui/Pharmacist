// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;

using Splat;

namespace Pharmacist.Core.Generation
{
    /// <summary>
    /// Extension methods associated with the System.Reflection.Metadata and ICSharpCode.Decompiler based classes.
    /// </summary>
    internal static class ReflectionExtensions
    {
        private static readonly ConcurrentDictionary<ICompilation, Dictionary<string, List<ITypeDefinition>>> _typeNameMapping = new();
        private static readonly ConcurrentDictionary<ICompilation, IEnumerable<ITypeDefinition>> _publicNonGenericTypeMapping = new();
        private static readonly ConcurrentDictionary<ICompilation, IEnumerable<ITypeDefinition>> _publicEventsTypeMapping = new();

        /// <summary>
        /// Get all type definitions where they have public events, aren't generic (no type parameters == 0), and they are public.
        /// </summary>
        /// <param name="compilation">The compilation unit.</param>
        /// <returns>A enumerable of type definitions that match the criteria.</returns>
        public static IEnumerable<ITypeDefinition> GetPublicTypesWithNotStaticEvents(this ICompilation compilation)
        {
            var list = GetPublicTypeDefinitionsWithEvents(compilation);
            return list
                .Where(x => x.Events.Any(eventDetails => !eventDetails.IsStatic));
        }

        /// <summary>
        /// Get all type definitions where they have public events, aren't generic (no type parameters == 0), and they are public.
        /// </summary>
        /// <param name="compilation">The compilation unit.</param>
        /// <returns>A enumerable of type definitions that match the criteria.</returns>
        public static IEnumerable<ITypeDefinition> GetPublicTypesWithStaticEvents(this ICompilation compilation)
        {
            var list = GetPublicTypeDefinitionsWithEvents(compilation);
            return list
                .Where(x => x.Events.Any(eventDetails => eventDetails.IsStatic));
        }

        /// <summary>
        /// Gets type definitions matching the full name and in the reference and main libraries.
        /// </summary>
        /// <param name="compilation">The compilation to scan.</param>
        /// <param name="name">The name of the item to get.</param>
        /// <returns>The name of the items.</returns>
        public static IReadOnlyCollection<ITypeDefinition> GetReferenceTypeDefinitionsWithFullName(this ICompilation compilation, string name)
        {
            var map = _typeNameMapping.GetOrAdd(compilation, comp => comp.ReferencedModules.Concat(compilation.Modules).SelectMany(x => x.TypeDefinitions).GroupBy(x => x.ReflectionName).ToDictionary(x => x.Key, x => x.ToList()));

            if (map.TryGetValue(name, out var value))
            {
                return value;
            }

            return Array.Empty<ITypeDefinition>();
        }

        /// <summary>
        /// Get a list of non-generic public type definitions.
        /// </summary>
        /// <param name="compilation">The compilation to get the type definitions from.</param>
        /// <returns>The list of type definitions.</returns>
        public static IEnumerable<ITypeDefinition> GetPublicTypeDefinitions(this ICompilation compilation)
        {
            return _publicNonGenericTypeMapping.GetOrAdd(
                compilation,
                comp =>
                {
                    var types = new HashSet<ITypeDefinition>(TypeDefinitionNameComparer.Default);
                    foreach (var item in comp.GetAllTypeDefinitions())
                    {
                        if (item.Accessibility != Accessibility.Public || item.TypeParameterCount > 0)
                        {
                            continue;
                        }

                        types.Add(item);
                    }

                    return types.ToList();
                });
        }

        /// <summary>
        /// Gets the type that the event.
        /// </summary>
        /// <param name="eventDetails">The details about the event.</param>
        /// <returns>The type of the event.</returns>
        public static IType? GetEventType(this IEvent eventDetails)
        {
            var compilation = eventDetails.Compilation;

            // Find the EventArgs type parameter of the event via digging around via reflection
            if (eventDetails.CanAdd && eventDetails.CanRemove)
            {
                return GetRealType(eventDetails.ReturnType, compilation);
            }

            LogHost.Default.Debug($"Type for {eventDetails.DeclaringType.FullName} is not valid");
            return null;
        }

        /// <summary>
        /// If the type if UnknownType, it will search all the dependencies for the specified type.
        /// If the type is not unknown type it will just return the type directly.
        /// This gets around the problem that types are only known for the assembly they are included in.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="compilation">The compilation which contains all the assemblies and dependencies.</param>
        /// <returns>The concrete type if one can be found.</returns>
        public static IType GetRealType(this IType type, ICompilation compilation)
        {
            // If the type is UnknownType, check other assemblies we have as dependencies first,
            // since UnknownType is only if it's unknown in the current assembly only.
            // This scenario is fairly common with types in the netstandard libraries, eg System.EventHandler.
            var newType = type;
            if (newType is not UnknownType && newType.Kind != TypeKind.Unknown)
            {
                return newType;
            }

            if (newType.TypeParameterCount == 0)
            {
                newType = compilation.GetReferenceTypeDefinitionsWithFullName(newType.ReflectionName).FirstOrDefault();
            }
            else if (newType is ParameterizedType paramType)
            {
                var genericType = compilation.GetReferenceTypeDefinitionsWithFullName(paramType.GenericType.ReflectionName).FirstOrDefault();

                var typeArguments = newType.TypeArguments.Select(x => GetRealType(x, compilation));

                newType = new ParameterizedType(genericType, typeArguments);
            }

            return newType ?? type;
        }

        /// <summary>
        /// Gets a string form of the type and generic arguments for a type.
        /// </summary>
        /// <param name="currentType">The type to generate the arguments for.</param>
        /// <returns>A type descriptor including the generic arguments.</returns>
        public static string GenerateFullGenericName(this IType currentType)
        {
            var (isBuiltIn, typeName) = GetBuiltInType(currentType.FullName);
            var sb = new StringBuilder(!isBuiltIn ? "global::" + typeName : typeName);

            if (currentType.TypeParameterCount <= 0)
            {
                return sb.ToString();
            }

            var isUnbound = currentType.IsUnbound();
            var arguments = string.Join(", ", currentType.TypeArguments.Select(GenerateName));
            sb.Append('<').Append(arguments).Append('>');

            string GenerateName(IType type) => isUnbound ? type.FullName : GenerateFullGenericName(type);

            return sb.ToString();
        }

        /// <summary>
        /// Checks if the specified generic type definition is unbound.
        /// </summary>
        /// <param name="definition">The type to check properties for.</param>
        /// <returns>Returns true if type is an unbound generic type, otherwise false.</returns>
        public static bool IsUnboundGenericTypeDefinition(this ITypeDefinition definition)
        {
            return definition.TypeParameterCount > 0 && definition.IsUnbound();
        }

        private static IEnumerable<ITypeDefinition> GetPublicTypeDefinitionsWithEvents(ICompilation compilation)
        {
            return _publicEventsTypeMapping.GetOrAdd(
                    compilation,
                    comp =>
                    {
                        return comp.GetPublicTypeDefinitions()
                            .Where(x =>
                                x.Events.Any(eventInfo => eventInfo.Accessibility == Accessibility.Public))
                            .ToList();
                    });
        }

        private static (bool IsInternalType, string TypeName) GetBuiltInType(string typeName)
        {
            return TypesMetadata.FullToBuiltInTypes.TryGetValue(typeName, out var builtInName) ? (true, builtInName) : (false, typeName);
        }
    }
}
