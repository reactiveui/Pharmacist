// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Pharmacist.Core.Groups;

namespace Pharmacist.Core.Extractors
{
    /// <summary>
    /// Extracts information from a platform, assembly or nuget package.
    /// </summary>
    public interface IExtractor
    {
        /// <summary>
        /// Gets the input for the generators and resolvers.
        /// </summary>
        InputAssembliesGroup? Input { get; }
    }
}
