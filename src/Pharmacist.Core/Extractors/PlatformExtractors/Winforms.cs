// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using NuGet.Frameworks;

using Pharmacist.Core.Groups;
using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Win Forms platform assemblies and events.
    /// </summary>
    internal class Winforms : NetFrameworkBase
    {
        private static readonly string[] WantedFileNames =
        {
            "System.dll",
            "System.Data.dll",
            "System.DirectoryServices.dll",
            "System.Messaging.dll",
            "System.Windows.Forms.dll",
            "System.Windows.Forms.DataVisualization.dll",
            "System.ServiceProcess.dll",
        };

        public Winforms(string filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.Winforms;

        /// <inheritdoc />
        protected override void SetFiles(InputAssembliesGroup folderGroups)
        {
            var fileMetadataEnumerable = folderGroups.IncludeGroup.GetAllFileNames().Where(file => WantedFileNames.Contains(Path.GetFileName(file), StringComparer.InvariantCultureIgnoreCase));
            Input.IncludeGroup.AddFiles(fileMetadataEnumerable);
        }
    }
}
