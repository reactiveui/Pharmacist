// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
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
    internal class WPF : NetCoreExtractorBase
    {
        public WPF(string? filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.WPF;

        /// <inheritdoc />
        protected override HashSet<string> WantedFileNames { get; } = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase)
        {
            "WindowsBase.dll",
            "PresentationCore.dll",
            "PresentationFramework.dll"
        };
    }
}
