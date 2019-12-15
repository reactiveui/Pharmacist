// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

using NuGet.Frameworks;

using Pharmacist.Core.ReferenceLocators;
using Pharmacist.Core.Utilities;

namespace Pharmacist.Core.NuGet
{
    internal static class NuGetFrameworkFolderHelper
    {
        /// <summary>
        /// Handles getting additional reference libraries where none exists.
        /// </summary>
        /// <param name="framework">The framework to analyze.</param>
        /// <returns>A list of additional paths.</returns>
        public static IEnumerable<string> GetNuGetFrameworkFolders(this NuGetFramework framework)
        {
            IEnumerable<string> folders;
            switch (framework.Framework.ToLowerInvariant())
            {
                case "monoandroid":
                    folders = HandleAndroid(framework);
                    break;
                case "xamarin.ios":
                    folders = HandleiOS();
                    break;
                case "xamarin.tvos":
                    folders = HandleTVOS();
                    break;
                case "xamarin.watchos":
                    folders = HandleWatchOS();
                    break;
                case "xamarin.mac":
                    folders = HandleMac();
                    break;
                default:
                    folders = Array.Empty<string>();
                    break;
            }

            return FileSystemHelpers.GetSubdirectoriesWithMatch(folders, AssemblyHelpers.AssemblyFileExtensionsSet);
        }

        private static IEnumerable<string> HandleWatchOS()
        {
            var referenceAssembliesLocation = GetReferenceLocation("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/");

            return new[] { Path.Combine(referenceAssembliesLocation, "Xamarin.WatchOS") };
        }

        private static IEnumerable<string> HandleTVOS()
        {
            var referenceAssembliesLocation = GetReferenceLocation("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/");

            return new[] { Path.Combine(referenceAssembliesLocation, "Xamarin.TVOS") };
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "iOS special naming scheme.")]
        private static IEnumerable<string> HandleiOS()
        {
            var referenceAssembliesLocation = GetReferenceLocation("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/lib/mono/");

            return new[] { Path.Combine(referenceAssembliesLocation, "Xamarin.iOS") };
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "iOS special naming scheme.")]
        private static IEnumerable<string> HandleMac()
        {
            var referenceAssembliesLocation = GetReferenceLocation("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/");

            return new[] { Path.Combine(referenceAssembliesLocation, "Xamarin.Mac") };
        }

        private static IEnumerable<string> HandleAndroid(NuGetFramework nugetFramework)
        {
            var referenceAssembliesLocation = GetReferenceLocation("/Library/Frameworks/Xamarin.Android.framework/Versions/Current/lib/xamarin.android/xbuild-frameworks");

            var versionText = $"v{nugetFramework.Version.Major}.{nugetFramework.Version.Minor}";

            return new[] { Path.Combine(referenceAssembliesLocation, "MonoAndroid", versionText), Path.Combine(referenceAssembliesLocation, "MonoAndroid", "v1.0") };
        }

        private static string GetReferenceLocation(string macLocation)
        {
            string referenceAssembliesLocation;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                referenceAssembliesLocation = macLocation;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                referenceAssembliesLocation = ReferenceLocator.GetReferenceLocation();
            }
            else
            {
                throw new NotSupportedException("Cannot process on Linux");
            }

            return referenceAssembliesLocation;
        }
    }
}
