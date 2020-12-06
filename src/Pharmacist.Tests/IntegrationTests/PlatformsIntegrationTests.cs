// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static IEnumerable<string> Frameworks { get; } = new string[]
                                                                {
                                                                    "Xamarin.Mac20",
                                                                    "net471",
                                                                    "net472",
                                                                    "net48",
                                                                    "net461",
                                                                    "net462",
                                                                    "netcoreapp3.0",
                                                                    "netcoreapp3.1",
                                                                    "net5.0",
                                                                    "Xamarin.WATCHOS10",
                                                                    "Xamarin.iOS10",
                                                                    "uap10.0.16299",
                                                                    "uap10.0.17763",
                                                                    "uap10.0.19041"
                                                                };

        /// <summary>
        /// The tests to perform.
        /// </summary>
        /// <returns>The test data.</returns>
        public static IEnumerable<object[]> GetPlatforms()
        {
            foreach (var platform in Frameworks.Where(x => x.StartsWith("net", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new object[] { platform, true, true };
                yield return new object[] { platform, false, true };
                yield return new object[] { platform, true, false };
            }

            foreach (var platform in Frameworks.Where(x => !x.StartsWith("net", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new object[] { platform, false, false };
            }
        }

        /// <summary>
        /// Tests to make sure that the platform tests produce valid output.
        /// </summary>
        /// <param name="platform">The platform to test.</param>
        /// <param name="isWpf">If to generate WPF.</param>
        /// <param name="isWinforms">If to generate WinForms.</param>
        /// <returns>A task to monitor the progress.</returns>
        [Theory]
        [MemberData(nameof(GetPlatforms))]
        public async Task PlatformGeneratesCode(string platform, bool isWpf, bool isWinforms)
        {
            if (platform == null)
            {
                throw new ArgumentNullException(nameof(platform));
            }

            var sourceDirectory = Path.Combine(IntegrationTestHelper.GetOutputDirectory(), "Platforms");

            if (!Directory.Exists(sourceDirectory))
            {
                Directory.CreateDirectory(sourceDirectory);
            }

            var referenceAssembliesLocation = ReferenceLocator.GetReferenceLocation();

            var nameSuffix = string.Empty;

            if (isWpf)
            {
                nameSuffix += ".wpf";
            }

            if (isWinforms)
            {
                nameSuffix += ".winforms";
            }

            var receivedSuffix = nameSuffix + ".received.txt";
            var receivedFileName = Path.Combine(sourceDirectory, platform.ToLowerInvariant() + receivedSuffix);
            var approvedSuffix = nameSuffix + ".approved.txt";
            var approvedFileName = Path.Combine(sourceDirectory, platform.ToLowerInvariant() + approvedSuffix);

            await ObservablesForEventGenerator.ExtractEventsFromPlatforms(sourceDirectory, string.Empty, receivedSuffix, referenceAssembliesLocation, platform, isWpf, isWinforms, TestUtilities.GetPackageDirectory(), false).ConfigureAwait(false);
        }
    }
}
