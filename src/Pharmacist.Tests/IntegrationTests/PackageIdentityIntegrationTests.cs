// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

using Xunit;

namespace Pharmacist.Tests.IntegrationTests
{
    /// <summary>
    /// Tests to make sure that integration tests produce correct results.
    /// </summary>
    public class PackageIdentityIntegrationTests
    {
        /// <summary>
        /// Tests to make sure the Tizen platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task TizenNuGetTest()
        {
            var package = new[] { new PackageIdentity("Tizen.NET.API4", new NuGetVersion("4.0.1.14152")) };
            var frameworks = new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };

            return IntegrationTestHelper.CheckResultsAgainstTemplate(package, frameworks);
        }

        /// <summary>
        /// Tests to make sure the Xamarin.Essentials platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task XamarinEssentialsNuGetTest()
        {
            var package = new[] { new PackageIdentity("Xamarin.Essentials", new NuGetVersion("1.1.0")) };
            var frameworks = "MonoAndroid81".ToFrameworks();

            return IntegrationTestHelper.CheckResultsAgainstTemplate(package, frameworks);
        }

        /// <summary>
        /// Tests to make sure the Xamarin.Forms platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task XamarinFormsNuGetTest()
        {
            var package = new[] { new PackageIdentity("Xamarin.Forms", new NuGetVersion("4.0.0.482894")) };
            var frameworks = "MonoAndroid81".ToFrameworks();

            return IntegrationTestHelper.CheckResultsAgainstTemplate(package, frameworks);
        }
    }
}
