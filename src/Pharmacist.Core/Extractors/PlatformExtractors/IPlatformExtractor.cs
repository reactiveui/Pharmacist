// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using NuGet.Frameworks;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Interface representing a platform assemblies and events.
    /// </summary>
    public interface IPlatformExtractor : IExtractor
    {
        /// <summary>
        /// If the extractor can extract for the framework.
        /// </summary>
        /// <param name="frameworks">The framework to check.</param>
        /// <returns>If the extraction works.</returns>
        bool CanExtract(NuGetFramework[] frameworks);

        /// <summary>
        /// Extract details about the platform.
        /// </summary>
        /// <param name="frameworks">The frameworks to extract for.</param>
        /// <param name="referenceAssembliesLocation">The location for reference assemblies if needed.</param>
        /// <returns>A task to monitor the progress.</returns>
        Task Extract(NuGetFramework[] frameworks, string referenceAssembliesLocation);
    }
}
