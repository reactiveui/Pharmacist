// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using NuGet.Frameworks;

using Pharmacist.Core.ReferenceLocators;

namespace Pharmacist.Core.NuGet
{
    internal static class NuGetFrameworkFolderHelper
    {
        /// <summary>
        /// Handles getting additional reference libraries where none exists.
        /// </summary>
        /// <param name="framework">The framework to analyze.</param>
        /// <returns>A list of additional paths.</returns>
        public static async Task<IEnumerable<string>> GetNuGetFrameworkFolders(this NuGetFramework framework)
        {
            switch (framework.Framework.ToLowerInvariant())
            {
                case "monoandroid":
                    return await HandleAndroid(framework).ConfigureAwait(false);
                case "xamarin.ios":
                    return await HandleiOS().ConfigureAwait(false);
                case "xamarin.tvos":
                    return await HandleTVOS().ConfigureAwait(false);
                case "xamarin.watchos":
                    return await HandleWatchOS().ConfigureAwait(false);
            }

            return Array.Empty<string>();
        }

        private static async Task<IEnumerable<string>> HandleWatchOS()
        {
            string referenceAssembliesLocation = await GetReferenceLocation("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/").ConfigureAwait(false);

            return new[] { Path.Combine(referenceAssembliesLocation, "Xamarin.WatchOS") };
        }

        private static async Task<IEnumerable<string>> HandleTVOS()
        {
            string referenceAssembliesLocation = await GetReferenceLocation("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/").ConfigureAwait(false);

            return new[] { Path.Combine(referenceAssembliesLocation, "Xamarin.TVOS") };
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "iOS special naming scheme.")]
        private static async Task<IEnumerable<string>> HandleiOS()
        {
            string referenceAssembliesLocation = await GetReferenceLocation("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/").ConfigureAwait(false);

            return new[] { Path.Combine(referenceAssembliesLocation, "Xamarin.iOS") };
        }

        private static async Task<IEnumerable<string>> HandleAndroid(NuGetFramework nugetFramework)
        {
            string referenceAssembliesLocation = await GetReferenceLocation("/Library/Frameworks/Xamarin.Android.framework/Libraries/xbuild-frameworks").ConfigureAwait(false);

            var versionText = $"v{nugetFramework.Version.Major}.{nugetFramework.Version.Minor}";

            return new[] { Path.Combine(referenceAssembliesLocation, "MonoAndroid", "v1.0"), Path.Combine(referenceAssembliesLocation, "MonoAndroid", versionText) };
        }

        private static async Task<string> GetReferenceLocation(string macLocation)
        {
            string referenceAssembliesLocation;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                referenceAssembliesLocation = macLocation;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                referenceAssembliesLocation = await ReferenceLocator.GetReferenceLocation().ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException("Cannot process on Linux");
            }

            return referenceAssembliesLocation;
        }
    }
}
