// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EventBuilder.Core.NuGet;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Serilog;

namespace EventBuilder.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Xamarin Essentials  platform.
    /// </summary>
    public class Essentials : NuGetExtractor, IPlatformExtractor
    {
        private readonly PackageIdentity[] _packageNames =
        {
            new PackageIdentity("Xamarin.Essentials", new NuGetVersion("1.1.0")),
        };

        private readonly PackageIdentity[] _supportPackages =
        {
            new PackageIdentity("NetStandard.Library", new NuGetVersion("2.0.3")),
        };

        /// <inheritdoc />
        public AutoPlatform Platform => AutoPlatform.Essentials;

        /// <inheritdoc />
        public Task Extract(string assemblyReferencePath)
        {
            return Extract(FrameworkConstants.CommonFrameworks.NetStandard20, _packageNames);
        }
    }
}
