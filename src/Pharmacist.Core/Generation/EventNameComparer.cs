// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler.TypeSystem;

namespace Pharmacist.Core.Generation
{
    internal class EventNameComparer : IComparer<IEvent>, IEqualityComparer<IEvent>
    {
        public static EventNameComparer Default { get; } = new();

        public int Compare(IEvent? x, IEvent? y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            return string.Compare(x.Name, y.Name, StringComparison.InvariantCulture);
        }

        public bool Equals(IEvent? x, IEvent? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return string.Equals(x.Name, y.Name, StringComparison.InvariantCulture);
        }

        public int GetHashCode(IEvent obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
