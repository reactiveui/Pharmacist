// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EventBuilder.Core.Reflection.Generators;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventBuilder.Core.Reflection.Resolvers
{
    /// <summary>
    /// A namespace resolver that extracts event information.
    /// </summary>
    internal abstract class EventNamespaceResolverBase : INamespaceResolver
    {
        private static readonly ImmutableHashSet<string> SkipNamespaceList = new[]
        {
            "Windows.UI.Xaml.Data",
            "Windows.UI.Xaml.Interop",
            "Windows.UI.Xaml.Input",
            "MonoTouch.AudioToolbox",
            "MonoMac.AudioToolbox",
            "ReactiveUI.Events",

            // Winforms
            "System.Collections.Specialized",
            "System.Configuration",
            "System.ComponentModel.Design",
            "System.ComponentModel.Design.Serialization",
            "System.CodeDom",
            "System.Data.SqlClient",
            "System.Data.OleDb",
            "System.Data.Odbc",
            "System.Data.Common",
            "System.Drawing.Design",
            "System.Media",
            "System.Net",
            "System.Net.Mail",
            "System.Net.NetworkInformation",
            "System.Net.Sockets",
            "System.ServiceProcess.Design",
            "System.Windows.Input",
            "System.Windows.Forms.ComponentModel.Com2Interop",
            "System.Windows.Forms.Design",
            "System.Timers"
        }.ToImmutableHashSet();

        /// <inheritdoc />
        public IEnumerable<NamespaceDeclarationSyntax> Create(ICompilation compilation)
        {
            var events = GetPublicTypesWithEvents(compilation)
                .Where(x => !SkipNamespaceList.Contains(x.Namespace))
                .Select(x => (x, GetValidEventDetails(x.Events)))
                .ToList();

            return GetEventGenerator().Generate(events);
        }

        protected abstract IEventGenerator GetEventGenerator();

        protected abstract IEnumerable<ITypeDefinition> GetPublicTypesWithEvents(ICompilation compilation);

        protected abstract IEnumerable<IEvent> GetValidEventDetails(IEnumerable<IEvent> eventDetails);
    }
}
