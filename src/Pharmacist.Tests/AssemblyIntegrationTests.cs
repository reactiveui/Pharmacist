// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core;
using Pharmacist.Core.NuGet;
using Shouldly;

using Xunit;

namespace Pharmacist.Tests
{
    /// <summary>
    /// Tests for testing different assemblies.
    /// </summary>
    public class AssemblyIntegrationTests
    {
        /// <summary>
        /// Tests to make sure the Tizen platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task IntegrationTestAssemblyTest()
        {
            using (var memoryStream = new MemoryStream())
            {
                var input = await NuGetPackageHelper.DownloadPackageFilesAndFolder(new[] { new PackageIdentity("NETStandard.Library", new NuGetVersion("2.0.0")) }).ConfigureAwait(false);

                using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, 1024, true))
                {
                    await ObservablesForEventGenerator.ExtractEventsFromAssemblies(streamWriter, input, FrameworkConstants.CommonFrameworks.NetStandard20).ConfigureAwait(false);
                }

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
