// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <inheritdoc />
    /// <summary>
    /// The Android platform.
    /// </summary>
    /// <seealso cref="BasePlatform" />
    public class Android : BasePlatform
    {
        private const string DesiredVersion = "v8.1";

        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.Android;

        /// <inheritdoc />
        public override Task Extract(string referenceAssembliesLocation)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                referenceAssembliesLocation = "/Library/Frameworks/Xamarin.Android.framework/Libraries/xbuild-frameworks";
            }

            // Pin to a particular framework version https://github.com/reactiveui/ReactiveUI/issues/1517
            var latestVersion = Directory.GetFiles(
                Path.Combine(referenceAssembliesLocation, "MonoAndroid"),
                "Mono.Android.dll",
                SearchOption.AllDirectories).Last(x => x.Contains(DesiredVersion));

            SearchDirectories = new[] { Path.Combine(referenceAssembliesLocation, "MonoAndroid", "v1.0"), Path.GetDirectoryName(latestVersion) };
            Assemblies = new[] { latestVersion };

            return Task.CompletedTask;
        }
    }
}
