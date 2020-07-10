// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Win Forms platform assemblies and events.
    /// </summary>
    internal class Winforms : NetCoreExtractorBase
    {
        public Winforms(string? filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.Winforms;

        /// <inheritdoc />
        protected override HashSet<string> WantedFileNames { get; } = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase)
        {
            "System.DirectoryServices.dll",
            "System.Windows.Forms.dll",
            "System.Drawing.dll",
        };
    }
}
