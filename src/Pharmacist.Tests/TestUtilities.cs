﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Pharmacist.Tests
{
    /// <summary>
    /// Some extension methods to make tests easier.
    /// </summary>
    public static class TestUtilities
    {
        private static readonly string NuGetBaseDirectory = Path.Combine(Path.GetTempPath(), "Pharmacist.Tests");

        [SuppressMessage("Design", "CA1031: Catch specific exceptions", Justification = "Test utility only.")]
        static TestUtilities()
        {
            try
            {
                Directory.Delete(NuGetBaseDirectory, true);
            }
            catch
            {
            }
        }

        public static string GetPackageDirectory([CallerMemberName]string callerMemberName = null, [CallerFilePath]string callerMemberFilePath = null) => Path.Combine(NuGetBaseDirectory, Path.GetFileName(callerMemberFilePath), callerMemberName);

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines", Justification = "Reviewed. Suppression is OK here.")]
        public static void ShouldHaveSameContents<T>(this IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var expectedValues = expected.ToList();
            var actualValues = actual.ToList();
            Assert.True(
                !expectedValues.Except(actualValues).Any() && expectedValues.Count == actualValues.Count,
                "Collections do not match." +
                $"{Environment.NewLine}Collection Actual Found:{Environment.NewLine}{string.Join(Environment.NewLine, actualValues)}" +
                $"{Environment.NewLine}Collection Expected:{Environment.NewLine}{string.Join(Environment.NewLine, expectedValues)}");
        }
    }
}
