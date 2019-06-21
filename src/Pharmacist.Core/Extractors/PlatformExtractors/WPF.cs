// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Pharmacist.Core.Groups;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// WPF platform assemblies and events.
    /// </summary>
    internal class WPF : NetFrameworkBase
    {
        private static readonly string[] WantedFileNames =
                                                          {
                                                              "WindowsBase.dll",
                                                              "PresentationCore.dll",
                                                              "PresentationFramework.dll"
                                                          };

        public WPF(string filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        public override AutoPlatform Platform { get; } = AutoPlatform.WPF;

        /// <inheritdoc />
        protected override void SetFiles(InputAssembliesGroup folderGroups)
        {
            var fileMetadataEnumerable = folderGroups.IncludeGroup.GetAllFileNames().Where(file => WantedFileNames.Contains(Path.GetFileName(file), StringComparer.InvariantCultureIgnoreCase));
            Input.IncludeGroup.AddFiles(fileMetadataEnumerable);
        }
    }
}
