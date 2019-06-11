// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// UWP platform assemblies and events.
    /// </summary>
    public class UWP : BasePlatform
    {
        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.UWP;

        /// <inheritdoc />
        public override Task Extract(string referenceAssembliesLocation)
        {
            if (PlatformHelper.IsRunningOnMono())
            {
                throw new NotSupportedException("Building events for UWP on Mac is not implemented yet.");
            }

            Assemblies = new[] { @"C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.17763.0\Windows.winmd" };
            SearchDirectories = new[] { @"C:\Windows\Microsoft.NET\Framework\v4.0.30319" };

            return Task.CompletedTask;
        }
    }
}
