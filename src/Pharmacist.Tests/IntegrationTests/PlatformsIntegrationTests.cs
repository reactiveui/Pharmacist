// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Pharmacist.Core;
using Pharmacist.Core.ReferenceLocators;

using Xunit;

namespace Pharmacist.Tests.IntegrationTests
{
    /// <summary>
    /// Tests associated with testing the platforms.
    /// </summary>
    public class PlatformsIntegrationTests
    {
        /// <summary>
        /// Tests to make sure that the platform tests produce valid output.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task TestsValid()
        {
            var sourceDirectory = IntegrationTestHelper.GetOutputDirectory();
            var referenceAssembliesLocation = await ReferenceLocator.GetReferenceLocation().ConfigureAwait(false);

            var platforms = Enum.GetValues(typeof(AutoPlatform)).Cast<AutoPlatform>().ToList();

            await ObservablesForEventGenerator.ExtractEventsFromPlatforms(sourceDirectory, string.Empty, ".received.txt", referenceAssembliesLocation, platforms).ConfigureAwait(false);

            foreach (var platform in platforms)
            {
                var approvedFileName = Path.Combine(sourceDirectory, $"{platform}.approved.txt");
                var receivedFileName = Path.Combine(sourceDirectory, $"{platform}.received.txt");

                IntegrationTestHelper.CheckContents(File.ReadAllText(receivedFileName), approvedFileName, receivedFileName);
            }
        }
    }
}
