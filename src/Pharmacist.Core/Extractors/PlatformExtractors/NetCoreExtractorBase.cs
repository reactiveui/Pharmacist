// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Versioning;
using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// WPF platform assemblies and events.
    /// </summary>
    internal abstract class NetCoreExtractorBase : NuGetExtractor, IPlatformExtractor
    {
        private static readonly IReadOnlyList<NuGetFramework> _frameworks = "netcoreapp3.1".ToFrameworks();
        private static readonly LibraryRange _windowsDesktopReference = new LibraryRange("Microsoft.WindowsDesktop.App.Ref", VersionRange.Parse("3.*"), LibraryDependencyTarget.Package);

        private readonly string? _filePath;

        public NetCoreExtractorBase(string? filePath)
        {
            _filePath = filePath ?? Path.GetTempPath();
        }

        /// <inheritdoc />
        public NuGetFramework Framework { get; } = _frameworks[0];

        /// <inheritdoc />
        public abstract AutoPlatform Platform { get; }

        /// <summary>
        /// Gets the wanted file names.
        /// </summary>
        protected abstract HashSet<string> WantedFileNames { get; }

        /// <inheritdoc />
        public async Task Extract(string referenceAssembliesLocation)
        {
            await Extract(_frameworks, new[] { _windowsDesktopReference }, _filePath).ConfigureAwait(false);

            if (Input == null)
            {
                return;
            }

            var fileMetadataEnumerable = Input.IncludeGroup.GetAllFileNames().Where(file => WantedFileNames.Contains(Path.GetFileName(file), StringComparer.InvariantCultureIgnoreCase));

            var newInput = new InputAssembliesGroup();
            newInput.IncludeGroup.AddFiles(fileMetadataEnumerable);
            newInput.SupportGroup.AddFiles(Input.IncludeGroup.GetAllFileNames());
            newInput.SupportGroup.AddFiles(Input.SupportGroup.GetAllFileNames());
            Input = newInput;
        }
    }
}
