// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using NuGet.Frameworks;

using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Mac platform assemblies and events.
    /// </summary>
    public class Mac : BasePlatform
    {
        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.Mac;

        /// <inheritdoc />
        public override NuGetFramework Framework { get; } = "Xamarin.Mac20".ToFrameworks()[0];

        /// <inheritdoc />
        public override Task Extract(string referenceAssembliesLocation)
        {
            Input = new InputAssembliesGroup();
            Input.IncludeGroup.AddFiles(FileSystemHelpers.GetFilesWithinSubdirectories(Framework.GetNuGetFrameworkFolders(), AssemblyHelpers.AssemblyFileExtensionsSet).Where(x => Path.GetFileName(x).IndexOf("Xamarin.Mac", StringComparison.InvariantCultureIgnoreCase) >= 0));

            return Task.CompletedTask;
        }
    }
}
