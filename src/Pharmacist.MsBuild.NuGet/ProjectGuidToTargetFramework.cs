// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using NuGet.Frameworks;

namespace Pharmacist.MsBuild.NuGet
{
    /// <summary>
    /// Converts the project guid format into a target framework value.
    /// </summary>
    internal static class ProjectGuidToTargetFramework
    {
        private static readonly Dictionary<Guid, string> _guidToFramework = new Dictionary<Guid, string>()
        {
            [new Guid("EFBA0AD7-5A72-4C68-AF49-83D382785DCF")] = "MonoAndroid",
            [new Guid("6BC8ED88-2882-458C-8E55-DFD12B67127B")] = "Xamarin.iOS",
            [new Guid("A5A43C5B-DE2A-4C0C-9213-0A381AF9435A")] = "uap",
            [new Guid("A3F8F2AB-B479-4A4A-A458-A89E7DC349F1")] = "Xamarin.Mac",
        };

        public static NuGetFramework GetTargetFramework(this IEnumerable<Guid> projectGuids, string projectVersionId)
        {
            foreach (var projectGuid in projectGuids)
            {
                if (_guidToFramework.TryGetValue(projectGuid, out var targetFrameworkValue))
                {
                    return new NuGetFramework(targetFrameworkValue, new Version(projectVersionId));
                }
            }

            return null;
        }
    }
}
