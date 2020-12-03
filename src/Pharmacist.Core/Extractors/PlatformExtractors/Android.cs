// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NuGet.Frameworks;

using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <inheritdoc />
    /// <summary>
    /// The Android platform.
    /// </summary>
    /// <seealso cref="BasePlatform" />
    public class Android : BasePlatform
    {
        /// <inheritdoc />
        public override bool CanExtract(NuGetFramework[] frameworks)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            return framework.Framework.StartsWith("MonoAndroid", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public override Task Extract(NuGetFramework[] frameworks, string referenceAssembliesLocation)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            Input.IncludeGroup.AddFiles(FileSystemHelpers.GetFilesWithinSubdirectories(framework.GetNuGetFrameworkFolders(), AssemblyHelpers.AssemblyFileExtensionsSet).Where(x => Path.GetFileName(x).Equals("Mono.Android.dll", StringComparison.InvariantCultureIgnoreCase)));
            return Task.CompletedTask;
        }
    }
}
