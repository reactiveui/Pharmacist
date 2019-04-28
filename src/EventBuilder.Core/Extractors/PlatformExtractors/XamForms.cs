// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace EventBuilder.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Xamarin Forms assemblies and events.
    /// </summary>
    public class XamForms : NuGetExtractor, IPlatformExtractor
    {
        private readonly PackageIdentity[] _packageNames = new[]
        {
            new PackageIdentity("Xamarin.Forms", new NuGetVersion("3.4.0.1039999")),
        };

        /// <inheritdoc />
        public AutoPlatform Platform => AutoPlatform.XamForms;

        /// <inheritdoc />
        public Task Extract(string referenceAssembliesLocation)
        {
            return Extract(FrameworkConstants.CommonFrameworks.NetStandard20, _packageNames);
        }
    }
}
