﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
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
        private static readonly Dictionary<string, IReadOnlyList<NuGetFramework>> _nugetFrameworks;

        /// <summary>
        /// Initializes static members of the <see cref="NuGetFrameworkHelper"/> class.
        /// </summary>
        static NuGetFrameworkHelper()
        {
            _nugetFrameworks = new Dictionary<string, IReadOnlyList<NuGetFramework>>(StringComparer.InvariantCultureIgnoreCase);
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
            _nugetFrameworks["Xamarin.iOS10"] = new[] { new NuGetFramework("Xamarin.iOS", new Version(1, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["Xamarin.Mac20"] = new[] { new NuGetFramework("Xamarin.Mac", new Version(2, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["Xamarin.TVOS10"] = new[] { new NuGetFramework("Xamarin.TVOS", new Version(1, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["Xamarin.WATCHOS10"] = new[] { new NuGetFramework("Xamarin.WATCHOS", new Version(1, 0, 0, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["net11"] = new[] { FrameworkConstants.CommonFrameworks.Net11 };

            _nugetFrameworks["net20"] = new[] { FrameworkConstants.CommonFrameworks.Net2 };
            _nugetFrameworks["net35"] = new[] { FrameworkConstants.CommonFrameworks.Net35 };
            _nugetFrameworks["net40"] = new[] { FrameworkConstants.CommonFrameworks.Net4 };
            _nugetFrameworks["net403"] = new[] { FrameworkConstants.CommonFrameworks.Net403 };
            _nugetFrameworks["net45"] = new[] { FrameworkConstants.CommonFrameworks.Net45 };
            _nugetFrameworks["net451"] = new[] { FrameworkConstants.CommonFrameworks.Net451 };
            _nugetFrameworks["net452"] = new[] { FrameworkConstants.CommonFrameworks.Net452 };
            _nugetFrameworks["net46"] = new[] { FrameworkConstants.CommonFrameworks.Net46 };
            _nugetFrameworks["net461"] = new[] { FrameworkConstants.CommonFrameworks.Net461 };
            _nugetFrameworks["net462"] = new[] { FrameworkConstants.CommonFrameworks.Net462 };
            _nugetFrameworks["net47"] = new[] { new NuGetFramework(".NETFramework", new Version(4, 7, 0, 0)) };
            _nugetFrameworks["net471"] = new[] { new NuGetFramework(".NETFramework", new Version(4, 7, 1, 0)) };
            _nugetFrameworks["net472"] = new[] { new NuGetFramework(".NETFramework", new Version(4, 7, 2, 0)) };
            _nugetFrameworks["net48"] = new[] { new NuGetFramework(".NETFramework", new Version(4, 8, 0, 0)) };

            _nugetFrameworks["uap10.0"] = new[] { FrameworkConstants.CommonFrameworks.UAP10, FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap"] = new[] { FrameworkConstants.CommonFrameworks.UAP10, FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.18362"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 18362, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.17763"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 17763, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.17134"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 17134, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.16299"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 16299, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.15063"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 15063, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.14393"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 14393, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.10586"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 10586, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };
            _nugetFrameworks["uap10.0.10240"] = new[] { new NuGetFramework("UAP", new Version(10, 0, 10240, 0)), FrameworkConstants.CommonFrameworks.NetStandard20 };

            _nugetFrameworks["Tizen40"] = new[] { FrameworkConstants.CommonFrameworks.Tizen4, FrameworkConstants.CommonFrameworks.NetStandard20 };
        }

        /// <summary>
        /// Extension method for getting the framework from the framework name.
        /// Ordered by the priority order.
        /// </summary>
        /// <param name="frameworkName">The name of the framework.</param>
        /// <returns>The framework.</returns>
        public static IReadOnlyList<NuGetFramework> ToFrameworks(this string frameworkName)
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
            if (framework == null)
            {
                throw new ArgumentNullException(nameof(framework));
            }

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
