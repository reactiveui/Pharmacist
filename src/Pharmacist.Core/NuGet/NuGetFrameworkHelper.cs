// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Pharmacist.Core.NuGet
{
    /// <summary>
    /// Helper class which will convert framework identifier strings to their nuget framework.
    /// </summary>
    public static class NuGetFrameworkHelper
    {
        private static readonly Dictionary<string, IReadOnlyCollection<NuGetFramework>> _nugetFrameworks;

        /// <summary>
        /// Initializes static members of the <see cref="NuGetFrameworkHelper"/> class.
        /// </summary>
        static NuGetFrameworkHelper()
        {
            _nugetFrameworks = new Dictionary<string, IReadOnlyCollection<NuGetFramework>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var property in typeof(FrameworkConstants.CommonFrameworks).GetProperties(BindingFlags.NonPublic | BindingFlags.Static))
            {
                _nugetFrameworks[property.Name] = new[] { (NuGetFramework)property.GetValue(null) };
            }

            // Some special cases for .net standard/.net app core since they require the '.' character in the numbers.
            _nugetFrameworks["NetStandard1.0"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard10 };
            _nugetFrameworks["NetStandard1.1"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard11 };
            _nugetFrameworks["NetStandard1.2"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard12 };
            _nugetFrameworks["NetStandard1.3"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard13 };
            _nugetFrameworks["NetStandard1.4"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard14 };
            _nugetFrameworks["NetStandard1.5"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard15 };
            _nugetFrameworks["NetStandard1.6"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard16 };
            _nugetFrameworks["NetStandard1.7"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard17 };
            _nugetFrameworks["NetStandard2.0"] = new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["NetStandard2.1"] = new[] { new NuGetFramework(".NETStandard", new Version(2, 1, 0, 0)) };
            _nugetFrameworks["UAP"] = new[] { FrameworkConstants.CommonFrameworks.UAP10 };
            _nugetFrameworks["UAP10.0"] = new[] { FrameworkConstants.CommonFrameworks.UAP10 };
            _nugetFrameworks["NetCoreApp1.0"] = new[] { FrameworkConstants.CommonFrameworks.NetCoreApp10 };
            _nugetFrameworks["NetCoreApp1.1"] = new[] { FrameworkConstants.CommonFrameworks.NetCoreApp11 };
            _nugetFrameworks["NetCoreApp2.0"] = new[] { FrameworkConstants.CommonFrameworks.NetCoreApp20 };
            _nugetFrameworks["NetCoreApp2.1"] = new[] { FrameworkConstants.CommonFrameworks.NetCoreApp21 };
            _nugetFrameworks["NetCoreApp2.2"] = new[] { new NuGetFramework(".NETCoreApp", new Version(2, 1, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["NetCoreApp3.0"] = new[] { new NuGetFramework(".NETCoreApp", new Version(3, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid50"] = new[] { new NuGetFramework("MonoAndroid", new Version(5, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid51"] = new[] { new NuGetFramework("MonoAndroid", new Version(5, 1, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid60"] = new[] { new NuGetFramework("MonoAndroid", new Version(6, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid70"] = new[] { new NuGetFramework("MonoAndroid", new Version(7, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid71"] = new[] { new NuGetFramework("MonoAndroid", new Version(7, 1, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid80"] = new[] { new NuGetFramework("MonoAndroid", new Version(8, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid81"] = new[] { new NuGetFramework("MonoAndroid", new Version(8, 1, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoAndroid90"] = new[] { new NuGetFramework("MonoAndroid", new Version(9, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["MonoTouch10"] = new[] { new NuGetFramework("MonoAndroid", new Version(1, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["Xamarin.iOS10"] = new[] { new NuGetFramework("Xamarin.iOS10", new Version(1, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["Xamarin.Mac20"] = new[] { new NuGetFramework("Xamarin.Mac20", new Version(2, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["Xamarin.TVOS10"] = new[] { new NuGetFramework("Xamarin.TVOS", new Version(1, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["Xamarin.WATCHOS10"] = new[] { new NuGetFramework("Xamarin.WATCHOS", new Version(1, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
        }

        /// <summary>
        /// Extension method for getting the framework from the framework name.
        /// Ordered by the priority order.
        /// </summary>
        /// <param name="frameworkName">The name of the framework.</param>
        /// <returns>The framework.</returns>
        public static IReadOnlyCollection<NuGetFramework> ToFrameworks(this string frameworkName)
        {
            _nugetFrameworks.TryGetValue(frameworkName, out var framework);

            return framework;
        }

        /// <summary>
        /// Gets a package identity if the framework is package based.
        /// </summary>
        /// <param name="framework">The framework to check.</param>
        /// <returns>The package details or null if none is available.</returns>
        public static IEnumerable<PackageIdentity> GetSupportLibraries(this NuGetFramework framework)
        {
            if (framework.Framework.StartsWith(".NETStandard", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { new PackageIdentity("NETStandard.Library", new NuGetVersion(framework.Version)) };
            }

            if (framework.Framework.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { new PackageIdentity("Microsoft.NETCore.App", new NuGetVersion(framework.Version)) };
            }

            if (framework.Framework.StartsWith("Tizen", StringComparison.OrdinalIgnoreCase))
            {
                if (framework.Version == new Version("4.0.0.0"))
                {
                    return new[]
                           {
                               new PackageIdentity("Tizen.NET.API4", new NuGetVersion("4.0.1.14152")),
                               new PackageIdentity("NETStandard.Library", new NuGetVersion("2.0.0.0"))
                           };
                }
            }

            if (framework.Framework.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { new PackageIdentity("Microsoft.NETFramework.ReferenceAssemblies", new NuGetVersion("1.0.0-preview.2")) };
            }

            if (framework.Framework.StartsWith("Mono", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { new PackageIdentity("NETStandard.Library", new NuGetVersion("2.0.0.0")) };
            }

            if (framework.Framework.StartsWith("Xamarin", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { new PackageIdentity("NETStandard.Library", new NuGetVersion("2.0.0.0")) };
            }

            return Array.Empty<PackageIdentity>();
        }
    }
}
