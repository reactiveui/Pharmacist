// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// WPF platform assemblies and events.
    /// </summary>
    internal class WPF : NetFrameworkBase
    {
        public WPF(string filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.WPF;

        /// <inheritdoc />
        protected override void SetFiles(string[] files)
        {
            var assemblies = new List<string>(10);
            assemblies.AddRange(files.Where(x => x.EndsWith("WindowsBase.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("PresentationCore.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("PresentationFramework.dll", StringComparison.InvariantCultureIgnoreCase)));
            Assemblies = assemblies;
        }
    }
}
