// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        public override AutoPlatform Platform => AutoPlatform.iOS;

        /// <inheritdoc />
        public override Task Extract(string referenceAssembliesLocation)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                referenceAssembliesLocation = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/";
            }

            var assemblies =
                Directory.GetFiles(
                    Path.Combine(referenceAssembliesLocation, "Xamarin.iOS"),
                    "Xamarin.iOS.dll",
                    SearchOption.AllDirectories);

            var latestVersion = assemblies.Last();
            Assemblies = new[] { latestVersion };
            SearchDirectories = new[] { Path.GetDirectoryName(latestVersion) };

            return Task.CompletedTask;
        }
    }
}
