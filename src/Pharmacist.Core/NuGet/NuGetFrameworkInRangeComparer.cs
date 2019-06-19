// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using NuGet.Frameworks;
using NuGet.Packaging.Core;

namespace Pharmacist.Core.NuGet
{
    internal class NuGetFrameworkInRangeComparer : IComparer<NuGetFramework>, IEqualityComparer<NuGetFramework>
    {
        public static NuGetFrameworkInRangeComparer Default { get; } = new NuGetFrameworkInRangeComparer();

        /// <inheritdoc />
        public bool Equals(NuGetFramework x, NuGetFramework y)
        {
            if (!NuGetFramework.FrameworkNameComparer.Equals(x, y))
            {
                return false;
            }

            return x.Version >= y.Version;
        }

        /// <inheritdoc />
        public int GetHashCode(NuGetFramework obj)
        {
            return NuGetFramework.FrameworkNameComparer.GetHashCode(obj);
        }

        /// <inheritdoc />
        public int Compare(NuGetFramework x, NuGetFramework y)
        {
            var result = StringComparer.OrdinalIgnoreCase.Compare(x.Framework, y.Framework);

            if (result != 0)
            {
                return result;
            }

            return x.Version.CompareTo(y.Version);
        }
    }
}
