// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Any library that based on the NET Framework references.
    /// </summary>
    internal abstract class NetFrameworkBase : BasePlatform
    {
        private static readonly PackageIdentity ReferenceNuGet = new PackageIdentity("Microsoft.NETFramework.ReferenceAssemblies.net461", new NuGetVersion("1.0.0-preview.2"));

        private static readonly NuGetFramework ReferenceFramework = FrameworkConstants.CommonFrameworks.Net461;

        private readonly string _filePath;

        protected NetFrameworkBase(string filePath)
        {
            _filePath = filePath;
        }

        /// <inheritdoc />
        public override NuGetFramework Framework { get; } = "net461".ToFrameworks()[0];

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Building events for WPF on Mac is not implemented.</exception>
        public override async Task Extract(string referenceAssembliesLocation)
        {
            var results = await NuGetPackageHelper.DownloadPackageFilesAndFolder(new[] { ReferenceNuGet }, new[] { ReferenceFramework }, packageOutputDirectory: _filePath).ConfigureAwait(false);

            Input = new InputAssembliesGroup { SupportGroup = results.SupportGroup };

            SetFiles(results);
        }

        /// <summary>
        /// Processes the files.
        /// </summary>
        /// <param name="folderGroups">The files.</param>
        protected abstract void SetFiles(InputAssembliesGroup folderGroups);
    }
}
