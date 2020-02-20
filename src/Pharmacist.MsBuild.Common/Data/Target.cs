// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Pharmacist.MsBuild.Common.Data
{
    /// <summary>
    /// Represents a target of a project asset.
    /// </summary>
    public class Target
    {
        /// <summary>
        /// Gets or sets the name of the package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the dependencies of the package.
        /// </summary>
        public List<string> Dependencies { get; set; }

        /// <summary>
        /// Gets or sets the files to be used in compilation.
        /// </summary>
        public List<string> Compiles { get; set; }

        /// <summary>
        /// Gets or sets the runtime files.
        /// </summary>
        public List<string> Runtimes { get; set; }

        /// <summary>
        /// Gets or sets the files contained within the package.
        /// </summary>
        public List<string> Files { get; set; }
    }
}
