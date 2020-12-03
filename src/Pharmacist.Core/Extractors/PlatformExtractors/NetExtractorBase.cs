// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
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
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// WPF platform assemblies and events.
    /// </summary>
    internal abstract class NetExtractorBase : NuGetExtractor, IPlatformExtractor
    {
        private const string NetFrameworkReferencePrefix = "Microsoft.NETFramework.ReferenceAssemblies";
        private const string WindowsDesktopRef = "Microsoft.WindowsDesktop.App.Ref";

        private readonly string? _filePath;

        protected NetExtractorBase(string? filePath)
        {
            _filePath = filePath ?? Path.GetTempPath();
        }

        /// <summary>
        /// Gets the wanted file names.
        /// </summary>
        protected abstract HashSet<string> WantedFileNames { get; }

        public async Task Extract(NuGetFramework[] frameworks, string referenceAssembliesLocation)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            var shortFileName = framework.GetShortFolderName();

            InputAssembliesGroup results;
            if (framework.IsDesktop())
            {
                var reference = new PackageIdentity(NetFrameworkReferencePrefix + '.' + shortFileName, new NuGetVersion("1.0.0"));

                results = await NuGetPackageHelper.DownloadPackageFilesAndFolder(new[] { reference }, frameworks, packageOutputDirectory: _filePath).ConfigureAwait(false);
            }
            else
            {
                var reference = new PackageIdentity(WindowsDesktopRef, new NuGetVersion(framework.Version));

                results = await NuGetPackageHelper.DownloadPackageFilesAndFolder(new[] { reference }, new[] { framework }, packageOutputDirectory: _filePath).ConfigureAwait(false);
            }

            Input = results;

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

        public abstract bool CanExtract(NuGetFramework[] frameworks);
    }
}
