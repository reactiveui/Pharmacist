// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using NuGet.Frameworks;

using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// UWP platform assemblies and events.
    /// </summary>
    public class UWP : BasePlatform
    {
        /// <inheritdoc />
        public override bool CanExtract(NuGetFramework[] frameworks)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            return framework.Framework.StartsWith("uap", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public override Task Extract(NuGetFramework[] frameworks, string referenceAssembliesLocation)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            if (PlatformHelper.IsRunningOnMono())
            {
                throw new NotSupportedException("Building events for UWP on Mac is not implemented yet.");
            }

            var metadataFile = AssemblyHelpers.FindUnionMetadataFile("Windows", framework.Version);

            if (metadataFile != null && !string.IsNullOrWhiteSpace(metadataFile))
            {
                Input.IncludeGroup.AddFiles(new[] { metadataFile });
            }

            return Task.CompletedTask;
        }
    }
}
