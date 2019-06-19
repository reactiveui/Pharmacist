// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

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

        public EventBuilderCompiler(IEnumerable<string> targetAssemblies, IEnumerable<string> searchDirectories)
        {
            var modules = targetAssemblies.Select(x => new PEFile(x, PEStreamOptions.PrefetchMetadata));
            _knownTypeCache = new KnownTypeCache(this);
            Init(modules, searchDirectories.ToList());
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

        private void Init(IEnumerable<IModuleReference> mainAssemblies, IReadOnlyCollection<string> searchDirectories)
        {
            if (mainAssemblies == null)
            {
                throw new ArgumentNullException(nameof(mainAssemblies));
            }

            if (searchDirectories == null)
            {
                throw new ArgumentNullException(nameof(searchDirectories));
            }

            var context = new SimpleTypeResolveContext(this);
            _assemblies.AddRange(mainAssemblies.Select(x => x.Resolve(context)));

            List<IModule> referencedAssemblies = new List<IModule>();

            var referenceModulesToProcess = new Stack<IAssemblyReference>(_assemblies.SelectMany(x => x.PEFile.AssemblyReferences));
            var assemblyReferencesVisited = new HashSet<string>();

            while (referenceModulesToProcess.Count > 0)
            {
                var currentAssemblyReference = referenceModulesToProcess.Pop();

                if (assemblyReferencesVisited.Contains(currentAssemblyReference.FullName))
                {
                    continue;
                }

                assemblyReferencesVisited.Add(currentAssemblyReference.FullName);

                var currentModules = currentAssemblyReference.Resolve(searchDirectories);

                if (currentModules.Count == 0)
                {
                    continue;
                }

                foreach (var currentModule in currentModules)
                {
                    IModule asm;
                    try
                    {
                        asm = ((IModuleReference)currentModule).Resolve(context);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new InvalidOperationException("Tried to initialize compilation with an invalid assembly reference. (Forgot to load the assembly reference ? - see CecilLoader)");
                    }

                    if (asm != null)
                    {
                        referencedAssemblies.Add(asm);
                        foreach (var element in asm.PEFile.AssemblyReferences)
                        {
                            referenceModulesToProcess.Push(element);
                        }
                    }
                }
            }

            _referencedAssemblies.AddRange(referencedAssemblies);
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
