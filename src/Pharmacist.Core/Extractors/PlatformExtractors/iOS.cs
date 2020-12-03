// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
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
    /// iOS platform assemblies and events.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "iOS special naming scheme.")]
    public class iOS : BasePlatform
    {
        /// <inheritdoc />
        public override bool CanExtract(NuGetFramework[] frameworks)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            return framework.Framework.StartsWith("Xamarin.iOS", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        [SuppressMessage("Usage", "CA2249:Consider using 'string.Contains' instead of 'string.IndexOf'", Justification = "Not supported on all platforms.")]
        public override Task Extract(NuGetFramework[] frameworks, string referenceAssembliesLocation)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            Input.IncludeGroup.AddFiles(FileSystemHelpers.GetFilesWithinSubdirectories(framework.GetNuGetFrameworkFolders(), AssemblyHelpers.AssemblyFileExtensionsSet).Where(x => Path.GetFileName(x).IndexOf("Xamarin.iOS", StringComparison.InvariantCultureIgnoreCase) >= 0));
            return Task.CompletedTask;
        }
    }
}
