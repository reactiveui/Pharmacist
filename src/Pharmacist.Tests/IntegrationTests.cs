// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core;
using Pharmacist.Core.NuGet;
using Pharmacist.IntegrationTest;

using Shouldly;

using Xunit;

namespace Pharmacist.Tests
{
    /// <summary>
    /// Tests to make sure that integration tests produce correct results.
    /// </summary>
    public class IntegrationTests
    {
        /// <summary>
        /// Tests to make sure the Tizen platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task TizenNuGetTest()
        {
            var package = new PackageIdentity("Tizen.NET.API4", new NuGetVersion("4.0.1.14152"));
            var framework = FrameworkConstants.CommonFrameworks.NetStandard20;

            using (var memoryStream = new MemoryStream())
            {
                await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(memoryStream, package, framework).ConfigureAwait(false);
                memoryStream.Flush();

                using (var sr = new StreamReader(memoryStream))
                {
                    var contents = sr.ReadToEnd();

                    contents.ShouldNotBeEmpty();
                }
            }
        }

        /// <summary>
        /// Tests to make sure the Tizen platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task IntegrationTestAssemblyTest()
        {
            using (var memoryStream = new MemoryStream())
            {
                var folders = (await NuGetPackageHelper.DownloadPackageAndGetLibFilesAndFolder(new PackageIdentity("NETStandard.Library", new NuGetVersion("2.0.0"))).ConfigureAwait(false)).Select(x => x.folder);

                await ObservablesForEventGenerator.ExtractEventsFromAssemblies(memoryStream, new[] { typeof(InstanceClass).Assembly.Location }, folders).ConfigureAwait(false);
                memoryStream.Flush();

                memoryStream.Position = 0;
                using (var sr = new StreamReader(memoryStream))
                {
                    var contents = sr.ReadToEnd();

                    contents.ShouldNotBeEmpty();
                }
            }
        }
    }
}
