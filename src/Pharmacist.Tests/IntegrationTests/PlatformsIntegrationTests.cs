// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
        /// <param name="autoPlatform">The platform to test.</param>
        /// <returns>A task to monitor the progress.</returns>
        [Theory]
        [InlineData(AutoPlatform.Winforms)]
        [InlineData(AutoPlatform.Android)]
        [InlineData(AutoPlatform.Mac)]
        [InlineData(AutoPlatform.TVOS)]
        [InlineData(AutoPlatform.UWP, Skip = "Failing")]
        [InlineData(AutoPlatform.WPF)]
        [InlineData(AutoPlatform.iOS)]
        public async Task PlatformGeneratesCode(AutoPlatform autoPlatform)
        {
            var sourceDirectory = IntegrationTestHelper.GetOutputDirectory();
            var referenceAssembliesLocation = ReferenceLocator.GetReferenceLocation();

            await ObservablesForEventGenerator.ExtractEventsFromPlatforms(sourceDirectory, string.Empty, ".received.txt", referenceAssembliesLocation, new[] { autoPlatform }, TestUtilities.GetPackageDirectory()).ConfigureAwait(false);
        }
    }
}
