// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Base platform.
    /// </summary>
    public abstract class BasePlatform : IPlatformExtractor
    {
        /// <inheritdoc />
        public abstract AutoPlatform Platform { get; }

        /// <inheritdoc />
        public IEnumerable<string> Assemblies { get; protected set; }

        /// <inheritdoc />
        public IEnumerable<string> SearchDirectories { get; protected set; }

        /// <inheritdoc />
        public abstract Task Extract(string referenceAssembliesLocation);
    }
}
