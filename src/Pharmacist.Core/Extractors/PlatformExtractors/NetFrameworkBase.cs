// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Any library that based on the NET Framework references.
    /// </summary>
    internal abstract class NetFrameworkBase : BasePlatform
    {
        private static readonly PackageIdentity ReferenceNuGet = new PackageIdentity("Microsoft.NETFramework.ReferenceAssemblies", new NuGetVersion("1.0.0-preview.2"));

        private static readonly NuGetFramework ReferenceFramework = FrameworkConstants.CommonFrameworks.Net461;

        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.WPF;

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">Building events for WPF on Mac is not implemented.</exception>
        public override async Task Extract(string referenceAssembliesLocation)
        {
            var results = await NuGetPackageHelper.DownloadPackageAndFilesAndFolder(new[] { ReferenceNuGet }, new[] { ReferenceFramework }).ConfigureAwait(false);
            var files = results.SelectMany(x => x.files).Where(x => x.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)).ToArray();
            SetFiles(files);
            SearchDirectories = new List<string>(results.Select(x => x.folder));
        }

        /// <summary>
        /// Processes the files.
        /// </summary>
        /// <param name="files">The files.</param>
        protected abstract void SetFiles(string[] files);
    }
}
