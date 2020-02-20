// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Frameworks;
using Pharmacist.Core.NuGet;

namespace Pharmacist.MsBuild.Common
{
    /// <summary>
    /// A helper for getting the TargetFramework based on elements inside a csproj file.
    /// </summary>
    public static class TargetFrameworkHelper
    {
        private static readonly Regex _versionRegex = new Regex(@"(\d+\.)?(\d+\.)?(\d+\.)?(\*|\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Dictionary<Guid, string> _guidToFramework = new Dictionary<Guid, string>()
        {
            [new Guid("EFBA0AD7-5A72-4C68-AF49-83D382785DCF")] = "MonoAndroid",
            [new Guid("6BC8ED88-2882-458C-8E55-DFD12B67127B")] = "Xamarin.iOS",
            [new Guid("A5A43C5B-DE2A-4C0C-9213-0A381AF9435A")] = "uap",
            [new Guid("A3F8F2AB-B479-4A4A-A458-A89E7DC349F1")] = "Xamarin.Mac",
        };

        /// <summary>
        /// Generates the NuGet target frameworks specified for a project.
        /// It will favour 2017 style csproj first then will check older style.
        /// </summary>
        /// <param name="targetFramework">The string with the TargetFramework element.</param>
        /// <param name="targetFrameworkVersion">The string with the TargetFrameworkVersion element.</param>
        /// <param name="targetPlatformVersion">The string with the TargetPlatformVersion element.</param>
        /// <param name="projectTypeGuids">The string with the ProjectTypeGuids element.</param>
        /// <returns>The NuGet target frameworks that match the specified target framework input.</returns>
        public static IReadOnlyList<NuGetFramework> GetTargetFrameworks(
            string targetFramework,
            string targetFrameworkVersion,
            string targetPlatformVersion,
            string projectTypeGuids)
        {
            if (!string.IsNullOrWhiteSpace(targetFramework))
            {
                return targetFramework.ToFrameworks();
            }

            var nugetFrameworks = new List<NuGetFramework>();

            if (string.IsNullOrWhiteSpace(projectTypeGuids))
            {
                return null;
            }

            var projectGuids = projectTypeGuids
                .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => new Guid(x.Trim()));

            var versionText = string.IsNullOrWhiteSpace(targetFrameworkVersion) ? targetFrameworkVersion : targetPlatformVersion;
            foreach (var projectGuid in projectGuids)
            {
                if (_guidToFramework.TryGetValue(projectGuid, out var targetFrameworkValue))
                {
                    var versionMatch = new Version(_versionRegex.Match(versionText).Value);
                    nugetFrameworks.Add(new NuGetFramework(targetFrameworkValue, versionMatch));
                }
            }

            return nugetFrameworks;
        }
    }
}
