// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pharmacist.Core.Generation
{
    /// <summary>
    ///     Contains metadata about types.
    /// </summary>
    internal static class TypesMetadata
    {
        /// <summary>
        /// Gets a set of CSharp keywords.
        /// </summary>
        public static ISet<string> CSharpKeywords { get; } = new HashSet<string>
        {
             "abstract",
             "as",
             "base",
             "bool",
             "break",
             "byte",
             "case",
             "catch",
             "char",
             "checked",
             "class",
             "const",
             "continue",
             "decimal",
             "default",
             "delegate",
             "do",
             "double",
             "else",
             "enum",
             "event",
             "explicit",
             "extern",
             "false",
             "finally",
             "fixed",
             "float",
             "for",
             "foreach",
             "goto",
             "if",
             "implicit",
             "in",
             "int",
             "interface",
             "internal",
             "is",
             "lock",
             "long",
             "namespace",
             "new",
             "null",
             "object",
             "operator",
             "out",
             "override",
             "params",
             "private",
             "protected",
             "public",
             "readonly",
             "ref",
             "return",
             "sbyte",
             "sealed",
             "short",
             "sizeof",
             "stackalloc",
             "static",
             "string",
             "struct",
             "switch",
             "this",
             "throw",
             "true",
             "try",
             "typeof",
             "uint",
             "ulong",
             "unchecked",
             "unsafe",
             "ushort",
             "using",
             "using",
             "static",
             "virtual void",
             "volatile",
             "while"
        };

        /// <summary>
        ///     Gets a list of full type names, and their built in C# aliases.
        /// </summary>
        public static IReadOnlyDictionary<string, string> FullToBuiltInTypes { get; } = new ReadOnlyDictionary<string, string>(
            new ConcurrentDictionary<string, string>
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
                ["System.String"] = "string"
            });
    }
}
