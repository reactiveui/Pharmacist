// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pharmacist.Core.Extractors.PlatformExtractors
{
    /// <summary>
    /// Win Forms platform assemblies and events.
    /// </summary>
    internal class Winforms : NetFrameworkBase
    {
        public Winforms(string filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        public override AutoPlatform Platform => AutoPlatform.Winforms;

        /// <inheritdoc />
        protected override void SetFiles(string[] files)
        {
            var assemblies = new List<string>(10);
            assemblies.AddRange(files.Where(x => x.EndsWith("System.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("System.Data.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("System.DirectoryServices.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("System.Messaging.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("System.Windows.Forms.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("System.Windows.Forms.DataVisualization.dll", StringComparison.InvariantCultureIgnoreCase)));
            assemblies.AddRange(files.Where(x => x.EndsWith("System.ServiceProcess.dll", StringComparison.InvariantCultureIgnoreCase)));
            Assemblies = assemblies;
        }
    }
}
