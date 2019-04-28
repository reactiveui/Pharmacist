// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

using EventBuilder.Core.NuGet;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Serilog;

namespace EventBuilder.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Tizen platform assemblies and events.
    /// </summary>
    public class Tizen : NuGetExtractor, IPlatformExtractor
    {
        private readonly PackageIdentity[] _packageNames = new[]
        {
            new PackageIdentity("Tizen.Net", new NuGetVersion("5.0.0.14562")),
        };

        /// <inheritdoc />
        public AutoPlatform Platform => AutoPlatform.Tizen4;

        /// <inheritdoc />
        public Task Extract(string referenceAssembliesLocation)
        {
            return Extract(FrameworkConstants.CommonFrameworks.NetStandard20, _packageNames);
        }
    }
}
