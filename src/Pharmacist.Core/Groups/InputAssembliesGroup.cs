// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Pharmacist.Core.Groups
{
    /// <summary>
    /// A series of folders and files for processing.
    /// </summary>
    public class InputAssembliesGroup
    {
        /// <summary>
        /// Gets a folder group which should contain the inclusions.
        /// </summary>
        public FilesGroup IncludeGroup { get; internal set; } = new FilesGroup();

        /// <summary>
        /// Gets a folder group with folders for including for support files only.
        /// </summary>
        public FilesGroup SupportGroup { get; internal set; } = new FilesGroup();
    }
}
