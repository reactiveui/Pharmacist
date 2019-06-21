// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

using NuGet.Frameworks;

using Pharmacist.Core.Comparers;
using Pharmacist.Core.Groups;
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Generation.Compilation
{
    /// <summary>
    /// This class is based on ICSharpCode.Decompiler SimpleCompiler.
    /// This has been changed to allow searching through reference types.
    /// </summary>
    /// <summary>
    /// Simple compilation implementation.
    /// </summary>
    internal sealed class EventBuilderCompiler : ICompilation, IDisposable
    {
        private readonly KnownTypeCache _knownTypeCache;
        private readonly List<IModule> _assemblies = new List<IModule>();
        private readonly List<IModule> _referencedAssemblies = new List<IModule>();
        private bool _initialized;
        private INamespace _rootNamespace;

        public EventBuilderCompiler(InputAssembliesGroup input, NuGetFramework framework)
        {
            _knownTypeCache = new KnownTypeCache(this);
            Init(input, framework);
        }

        /// <summary>
        /// Gets the main module we are extracting information from.
        /// This is mostly just here due to ILDecompile needing it.
        /// </summary>
        public IModule MainModule
        {
            get
            {
                if (!_initialized)
                {
                    throw new InvalidOperationException("Compilation isn't initialized yet");
                }

                return _assemblies.FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the modules we want to extract events from.
        /// </summary>
        public IReadOnlyList<IModule> Modules
        {
            get
            {
                if (!_initialized)
                {
                    throw new InvalidOperationException("Compilation isn't initialized yet");
                }

                return _assemblies;
            }
        }

        /// <summary>
        /// Gets the referenced modules. These are support modules where we want additional information about types from.
        /// This will likely be either the system reference libraries or .NET Standard libraries.
        /// </summary>
        public IReadOnlyList<IModule> ReferencedModules
        {
            get
            {
                if (!_initialized)
                {
                    throw new InvalidOperationException("Compilation isn't initialized yet");
                }

                return _referencedAssemblies;
            }
        }

        /// <summary>
        /// Gets the root namespace for our assemblies. We can start analyzing from here.
        /// </summary>
        public INamespace RootNamespace
        {
            get
            {
                INamespace ns = LazyInit.VolatileRead(ref _rootNamespace);
                if (ns != null)
                {
                    return ns;
                }

                if (!_initialized)
                {
                    throw new InvalidOperationException("Compilation isn't initialized yet");
                }

                return LazyInit.GetOrSet(ref _rootNamespace, CreateRootNamespace());
            }
        }

        /// <summary>
        /// Gets the comparer we are going to use for comparing names of items. We just compare ordinally.
        /// </summary>
        public StringComparer NameComparer => StringComparer.Ordinal;

        /// <summary>
        /// Gets the cache manager. This is mostly here for ILDecompile.
        /// </summary>
        public CacheManager CacheManager { get; } = new CacheManager();

        public INamespace GetNamespaceForExternAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                return RootNamespace;
            }

            // SimpleCompilation does not support extern aliases; but derived classes might.
            return null;
        }

        public IType FindType(KnownTypeCode typeCode)
        {
            return _knownTypeCache.FindType(typeCode);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var assembly in _assemblies)
            {
                assembly.PEFile.Dispose();
            }

            foreach (var referenceAssembly in _referencedAssemblies)
            {
                referenceAssembly.PEFile.Dispose();
            }
        }

        private static IEnumerable<IModule> GetReferenceModules(IEnumerable<IModule> mainModules, InputAssembliesGroup input, NuGetFramework framework, ITypeResolveContext context)
        {
            var assemblyReferencesSeen = new HashSet<IAssemblyReference>(AssemblyReferenceNameComparer.Default);

            var referenceModulesToProcess = new Stack<(IModule parent, IAssemblyReference reference)>(mainModules.SelectMany(x => x.PEFile.AssemblyReferences.Select(reference => (x, (IAssemblyReference)reference))));
            while (referenceModulesToProcess.Count > 0)
            {
                var current = referenceModulesToProcess.Pop();

                if (!assemblyReferencesSeen.Add(current.reference))
                {
                    continue;
                }

#pragma warning disable CA2000 // Dispose objects before losing scope
                var moduleReference = (IModuleReference)current.reference.Resolve(current.parent, input, framework);
#pragma warning restore CA2000 // Dispose objects before losing scope

                if (moduleReference == null)
                {
                    continue;
                }

                var module = moduleReference.Resolve(context);

                yield return module;

                foreach (var childAssemblyReference in module.PEFile.AssemblyReferences)
                {
                    referenceModulesToProcess.Push((module, childAssemblyReference));
                }
            }
        }

        private void Init(InputAssembliesGroup input, NuGetFramework framework)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var context = new SimpleTypeResolveContext(this);

            var moduleReferences = input.IncludeGroup.GetAllFileNames()
                .Where(file => AssemblyHelpers.AssemblyFileExtensionsSet.Contains(Path.GetExtension(file)))
                .Select(x => (IModuleReference)new PEFile(x, PEStreamOptions.PrefetchMetadata));

            _assemblies.AddRange(moduleReferences.Select(x => x.Resolve(context)));

            _referencedAssemblies.AddRange(GetReferenceModules(_assemblies, input, framework, context));

            _initialized = true;
        }

        private INamespace CreateRootNamespace()
        {
            var namespaces = new List<INamespace>();
            foreach (var module in _assemblies)
            {
                // SimpleCompilation does not support extern aliases; but derived classes might.
                // CreateRootNamespace() is virtual so that derived classes can change the global namespace.
                namespaces.Add(module.RootNamespace);
                for (int i = 0; i < _referencedAssemblies.Count; i++)
                {
                    namespaces.Add(_referencedAssemblies[i].RootNamespace);
                }
            }

            return new MergedNamespace(this, namespaces.ToArray());
        }
    }
}
