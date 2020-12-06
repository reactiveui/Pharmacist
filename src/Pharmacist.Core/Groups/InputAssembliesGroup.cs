// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

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
        public FilesGroup IncludeGroup { get; internal set; } = new();

        /// <summary>
        /// Gets a folder group with folders for including for support files only.
        /// </summary>
        public FilesGroup SupportGroup { get; internal set; } = new();

        /// <summary>
        /// Combines two groups together.
        /// </summary>
        /// <param name="firstGroup">The first group.</param>
        /// <param name="secondGroup">The second group.</param>
        /// <returns>The combined groups.</returns>
        public static InputAssembliesGroup Combine(InputAssembliesGroup firstGroup, InputAssembliesGroup secondGroup)
        {
            if (firstGroup == null)
            {
                throw new ArgumentNullException(nameof(firstGroup));
            }

            if (secondGroup == null)
            {
                throw new ArgumentNullException(nameof(secondGroup));
            }

            var newGroup = new InputAssembliesGroup();

            newGroup.IncludeGroup.AddFiles(firstGroup.IncludeGroup.GetAllFileNames());
            newGroup.IncludeGroup.AddFiles(secondGroup.IncludeGroup.GetAllFileNames());

            newGroup.SupportGroup.AddFiles(firstGroup.SupportGroup.GetAllFileNames());
            newGroup.SupportGroup.AddFiles(secondGroup.SupportGroup.GetAllFileNames());

            return newGroup;
        }

        /// <summary>
        /// Combines two groups together.
        /// </summary>
        /// <param name="secondGroup">The second group.</param>
        /// <returns>The combined groups.</returns>
        public InputAssembliesGroup Combine(InputAssembliesGroup secondGroup)
        {
            if (secondGroup == null)
            {
                throw new ArgumentNullException(nameof(secondGroup));
            }

            IncludeGroup.AddFiles(secondGroup.IncludeGroup.GetAllFileNames());

            SupportGroup.AddFiles(secondGroup.SupportGroup.GetAllFileNames());

            return this;
        }
    }
}
