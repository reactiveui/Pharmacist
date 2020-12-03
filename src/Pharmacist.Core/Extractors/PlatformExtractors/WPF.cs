// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.Frameworks;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// WPF platform assemblies and events.
    /// </summary>
    internal class WPF : NetExtractorBase
    {
        public WPF(string? filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        protected override HashSet<string> WantedFileNames { get; } = new(StringComparer.CurrentCultureIgnoreCase)
                                                                      {
                                                                          "WindowsBase.dll",
                                                                          "PresentationCore.dll",
                                                                          "PresentationFramework.dll"
                                                                      };

        public override bool CanExtract(NuGetFramework[] frameworks)
        {
            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var framework = frameworks[0];

            if (framework.Framework.Equals(".NETFramework", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (framework.Framework.Equals(".NETCoreApp", StringComparison.OrdinalIgnoreCase)
                && framework.Version > new Version(3, 1))
            {
                return true;
            }

            if (framework.Framework.Equals(FrameworkConstants.CommonFrameworks.Net50.Framework))
            {
                return true;
            }

            return false;
        }
    }
}
