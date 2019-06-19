// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using NuGet.Packaging.Core;

namespace Pharmacist.Core.NuGet
{
    internal class PackageIdentityNameComparer : IEqualityComparer<PackageIdentity>
    {
        public static PackageIdentityNameComparer Default { get; } = new PackageIdentityNameComparer();

        /// <inheritdoc />
        public bool Equals(PackageIdentity x, PackageIdentity y)
        {
            if (x == y)
            {
                return true;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(x?.Id, y?.Id);
        }

        /// <inheritdoc />
        public int GetHashCode(PackageIdentity obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Id);
        }
    }
}
