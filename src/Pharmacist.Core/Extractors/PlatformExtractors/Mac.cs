// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        public override Task Extract(string referenceAssembliesLocation)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                referenceAssembliesLocation = "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/";
            }

            var assemblies =
                Directory.GetFiles(
                    Path.Combine(referenceAssembliesLocation, "Xamarin.Mac"),
                    "Xamarin.Mac.dll",
                    SearchOption.AllDirectories);

            var latestVersion = assemblies.Last();
            Assemblies = new[] { latestVersion };
            SearchDirectories = new[] { Path.GetDirectoryName(latestVersion) };

            return Task.CompletedTask;
        }
    }
}
