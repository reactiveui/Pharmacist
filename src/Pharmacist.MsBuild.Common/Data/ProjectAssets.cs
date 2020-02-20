// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pharmacist.MsBuild.Common.Data
{
    /// <summary>
    /// Represents a project assets file.
    /// </summary>
    public class ProjectAssets
    {
        /// <summary>
        /// Gets or sets the version of the project assets.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the folders where packages are located.
        /// </summary>
        public List<Target> Targets { get; set; }
    }
}
