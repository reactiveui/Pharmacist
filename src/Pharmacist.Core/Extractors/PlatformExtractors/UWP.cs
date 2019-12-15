// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using NuGet.Frameworks;

using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// UWP platform assemblies and events.
    /// </summary>
    public class UWP : BasePlatform
    {
        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.UWP;

        /// <inheritdoc />
        public override NuGetFramework Framework { get; } = "uap10.0.16299".ToFrameworks()[0];

        /// <inheritdoc />
        public override Task Extract(string referenceAssembliesLocation)
        {
            if (PlatformHelper.IsRunningOnMono())
            {
                throw new NotSupportedException("Building events for UWP on Mac is not implemented yet.");
            }

            var metadataFile = AssemblyHelpers.FindUnionMetadataFile("Windows", Version.Parse("10.0.16299.0"));

            if (!string.IsNullOrWhiteSpace(metadataFile))
            {
                Input.IncludeGroup.AddFiles(new[] { metadataFile! });
            }

            return Task.CompletedTask;
        }
    }
}
