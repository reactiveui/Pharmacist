// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private static readonly ConcurrentDictionary<ICompilation, ImmutableDictionary<string, ImmutableList<ITypeDefinition>>> _typeNameMapping = new ConcurrentDictionary<ICompilation, ImmutableDictionary<string, ImmutableList<ITypeDefinition>>>();
        private static readonly ConcurrentDictionary<ICompilation, ImmutableList<ITypeDefinition>> _publicNonGenericTypeMapping = new ConcurrentDictionary<ICompilation, ImmutableList<ITypeDefinition>>();
        private static readonly ConcurrentDictionary<ICompilation, ImmutableList<ITypeDefinition>> _publicEventsTypeMapping = new ConcurrentDictionary<ICompilation, ImmutableList<ITypeDefinition>>();

        private static readonly ConcurrentDictionary<string, string> _fullToBuiltInTypes = new ConcurrentDictionary<string, string>
        {
            ["System.Boolean"] = "bool",
            ["System.Byte"] = "byte",
            ["System.SByte"] = "sbyte",
            ["System.Char"] = "char",
            ["System.Decimal"] = "decimal",
            ["System.Double"] = "double",
            ["System.Single"] = "float",
            ["System.Int32"] = "int",
            ["System.UInt32"] = "uint",
            ["System.Int64"] = "long",
            ["System.UInt64"] = "ulong",
            ["System.Object"] = "object",
            ["System.Int16"] = "short",
            ["System.UInt16"] = "ushort",
            ["System.String"] = "string",
        };

        /// <summary>
        /// Get all type definitions where they have public events, aren't generic (no type parameters == 0), and they are public.
        /// </summary>
        /// <param name="compilation">The compilation unit.</param>
        /// <returns>A enumerable of type definitions that match the criteria.</returns>
        public static IEnumerable<ITypeDefinition> GetPublicTypesWithNotStaticEvents(this ICompilation compilation)
        {
            var list = GetPublicTypeDefinitionsWithEvents(compilation);
            return list
                .Where(x => x.Events.Any(eventDetails => !eventDetails.IsStatic))
                .OrderBy(x => x.Namespace)
                .ThenBy(x => x.Name);
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
                .Where(x => x.Events.Any(eventDetails => eventDetails.IsStatic))
                .OrderBy(x => x.Namespace)
                .ThenBy(x => x.Name);
        }

        /// <summary>
        /// Gets type definitions matching the full name and in the reference and main libraries.
        /// </summary>
        /// <param name="compilation">The compilation to scan.</param>
        /// <param name="name">The name of the item to get.</param>
        /// <returns>The name of the items.</returns>
        public static IReadOnlyCollection<ITypeDefinition> GetReferenceTypeDefinitionsWithFullName(this ICompilation compilation, string name)
        {
            var map = _typeNameMapping.GetOrAdd(compilation, comp => comp.ReferencedModules.Concat(compilation.Modules).SelectMany(x => x.TypeDefinitions).GroupBy(x => x.ReflectionName).ToImmutableDictionary(x => x.Key, x => x.ToImmutableList()));

            return map.GetValueOrDefault(name);
        }

        public static string GetBuiltInType(string typeName)
        {
            if (_fullToBuiltInTypes.TryGetValue(typeName, out var builtInName))
            {
                return builtInName;
            }

            return typeName;
        }

        /// <summary>
        /// Get a list of non-generic public type definitions.
        /// </summary>
        /// <param name="compilation">The compilation to get the type definitions from.</param>
        /// <returns>The list of type definitions.</returns>
        public static IImmutableList<ITypeDefinition> GetPublicNonGenericTypeDefinitions(this ICompilation compilation)
        {
            return _publicNonGenericTypeMapping.GetOrAdd(
                    compilation,
                    comp => comp.GetAllTypeDefinitions().Where(x => x.Accessibility == Accessibility.Public && x.TypeParameterCount == 0).OrderBy(x => x.FullName)
                .ToImmutableList());
        }

        public static IType GetEventType(this IEvent eventDetails)
        {
            ICompilation compilation = eventDetails.Compilation;

            // Find the EventArgs type parameter of the event via digging around via reflection
            if (!eventDetails.CanAdd || !eventDetails.CanRemove)
            {
                LogHost.Default.Debug($"Type for {eventDetails.DeclaringType.FullName} is not valid");
                return null;
            }

            IType type = GetRealType(eventDetails.ReturnType, compilation);

            if (type == null)
            {
                LogHost.Default.Debug($"Type for {eventDetails.DeclaringType.FullName} is not valid");
                return null;
            }

            return type;
        }

        public static IType GetRealType(this IType type, ICompilation compilation)
        {
            if (type is UnknownType || type.Kind == TypeKind.Unknown)
            {
                if (type.TypeParameterCount == 0)
                {
                    type = compilation.GetReferenceTypeDefinitionsWithFullName(type.ReflectionName).FirstOrDefault();
                }
                else if (type is ParameterizedType paramType)
                {
                    var genericType = compilation.GetReferenceTypeDefinitionsWithFullName(paramType.GenericType.ReflectionName).FirstOrDefault();

                    var typeArguments = type.TypeArguments.Select(x => GetRealType(x, compilation));

                    type = new ParameterizedType(genericType, typeArguments);
                }
            }

            return type;
        }

        /// <summary>
        /// Gets a string form of the type and generic arguments for a type.
        /// </summary>
        /// <param name="currentType">The type to generate the arguments for.</param>
        /// <returns>A type descriptor including the generic arguments.</returns>
        public static string GenerateFullGenericName(this IType currentType)
        {
            return GenerateFullGenericName(currentType, true);
        }

        private static string GenerateFullGenericName(this IType currentType, bool isStart)
        {
            var currentTypeFullName = GetBuiltInType(currentType.FullName);
            var sb = new StringBuilder(isStart ? "global::" + currentTypeFullName : currentTypeFullName);

            if (currentType.TypeParameterCount > 0)
            {
                sb.Append("<")
                    .Append(string.Join(", ", currentType.TypeArguments.Select(x => GenerateFullGenericName(x, false))))
                    .Append(">");
            }

            return sb.ToString();
        }

        private static IImmutableList<ITypeDefinition> GetPublicTypeDefinitionsWithEvents(ICompilation compilation)
        {
            return _publicEventsTypeMapping.GetOrAdd(
                    compilation,
                    comp => comp.GetPublicNonGenericTypeDefinitions().Where(x => x.Events.Any(eventInfo => eventInfo.Accessibility == Accessibility.Public))
                .ToImmutableList());
        }
    }
}
