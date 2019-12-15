// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Pharmacist.Core.Utilities
{
    /// <summary>
    /// Extension methods for stack based operations.
    /// </summary>
    internal static class StackExtensions
    {
        public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                stack.Push(item);
            }
        }

        public static int TryPopRange<T>(this Stack<T> stack, T[] items)
        {
            return TryPopRange(stack, items, 0, items.Length);
        }

        public static int TryPopRange<T>(this Stack<T> stack, T[] items, int startIndex, int count)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "The count must be greater than 0.");
            }

            var length = items.Length;
            if (startIndex >= length || startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "The start index is out of range. It must between 0 and less than the length of the array.");
            }

            if (length - count < startIndex)
            {
                // instead of (startIndex + count > items.Length) to prevent overflow
                throw new ArgumentException("The start index is out of range. It must between 0 and less than the length of the array.");
            }

            if (count == 0)
            {
                return 0;
            }

            var nodesCount = stack.Count > count ? count : stack.Count;

            for (var i = startIndex; i < startIndex + nodesCount; i++)
            {
                items[i] = stack.Pop();
            }

            return nodesCount;
        }
    }
}
