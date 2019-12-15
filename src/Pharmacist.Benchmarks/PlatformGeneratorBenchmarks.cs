﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using Pharmacist.Core;

namespace Pharmacist.Benchmarks
{
    /// <summary>
    /// Benchmarks for the NavigationStack and the RoutingState objects.
    /// </summary>
    [ClrJob]
    ////[CoreJob]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class NavigationStackBenchmark
    {
        private static string _referenceAssembliesLocation = PlatformHelper.IsRunningOnMono() ?
    @"/Library⁩/Frameworks⁩/Libraries/⁨mono⁩" :
    @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\ReferenceAssemblies\Microsoft\Framework";

        /// <summary>
        /// Benchmark for when navigating to a new view model.
        /// </summary>
        [Benchmark]
        public Task Navigate()
        {
            return Task.CompletedTask;
        }
    }
}
