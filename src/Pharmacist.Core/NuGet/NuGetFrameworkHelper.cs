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
        private static readonly Dictionary<string, NuGetFramework> _nugetFrameworks;

        /// <summary>
        /// Initializes static members of the <see cref="NuGetFrameworkHelper"/> class.
        /// </summary>
        static NuGetFrameworkHelper()
        {
            _nugetFrameworks = new Dictionary<string, NuGetFramework>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var property in typeof(FrameworkConstants.CommonFrameworks).GetProperties(BindingFlags.NonPublic | BindingFlags.Static))
            {
                _nugetFrameworks[property.Name] = (NuGetFramework)property.GetValue(null);
            }

            // Some special cases for .net standard/.net app core since they require the '.' character in the numbers.
            _nugetFrameworks["NetStandard1.0"] = FrameworkConstants.CommonFrameworks.NetStandard10;
            _nugetFrameworks["NetStandard1.1"] = FrameworkConstants.CommonFrameworks.NetStandard11;
            _nugetFrameworks["NetStandard1.2"] = FrameworkConstants.CommonFrameworks.NetStandard12;
            _nugetFrameworks["NetStandard1.3"] = FrameworkConstants.CommonFrameworks.NetStandard13;
            _nugetFrameworks["NetStandard1.4"] = FrameworkConstants.CommonFrameworks.NetStandard14;
            _nugetFrameworks["NetStandard1.5"] = FrameworkConstants.CommonFrameworks.NetStandard15;
            _nugetFrameworks["NetStandard1.6"] = FrameworkConstants.CommonFrameworks.NetStandard16;
            _nugetFrameworks["NetStandard1.7"] = FrameworkConstants.CommonFrameworks.NetStandard17;
            _nugetFrameworks["NetStandard2.0"] = FrameworkConstants.CommonFrameworks.NetStandard20;
            _nugetFrameworks["NetStandard2.1"] = FrameworkConstants.CommonFrameworks.NetStandard21;
            _nugetFrameworks["UAP"] = FrameworkConstants.CommonFrameworks.UAP10;
            _nugetFrameworks["UAP10.0"] = FrameworkConstants.CommonFrameworks.UAP10;
            _nugetFrameworks["NetCoreApp1.0"] = FrameworkConstants.CommonFrameworks.NetCoreApp10;
            _nugetFrameworks["NetCoreApp1.1"] = FrameworkConstants.CommonFrameworks.NetCoreApp11;
            _nugetFrameworks["NetCoreApp2.0"] = FrameworkConstants.CommonFrameworks.NetCoreApp20;
            _nugetFrameworks["NetCoreApp2.1"] = FrameworkConstants.CommonFrameworks.NetCoreApp21;
            _nugetFrameworks["NetCoreApp2.2"] = FrameworkConstants.CommonFrameworks.NetCoreApp22;
            _nugetFrameworks["NetCoreApp3.0"] = FrameworkConstants.CommonFrameworks.NetCoreApp30;
        }

        /// <summary>
        /// Extension method for getting the framework from the framework name.
        /// </summary>
        /// <param name="frameworkName">The name of the framework.</param>
        /// <returns>The framework.</returns>
        public static NuGetFramework ToFramework(this string frameworkName)
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
            if (!framework.IsPackageBased)
            {
                return null;
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

            return null;
        }
    }
}
