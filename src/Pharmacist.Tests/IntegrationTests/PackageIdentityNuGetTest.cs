// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

using Xunit;

namespace Pharmacist.Tests.IntegrationTests
{
    /// <summary>
    /// Tests to make sure that integration tests produce correct results.
    /// </summary>
    public class PackageIdentityNuGetTest
    {
        [Theory]
        [InlineData("Xamarin.Forms", "4.3.0.991250", "MonoAndroid10.0")]
        [InlineData("Xamarin.Forms", "4.3.0.991250", "MonoAndroid90")]
        [InlineData("Xamarin.Forms", "4.3.0.991250", "tizen40")]
        [InlineData("Xamarin.Forms", "4.3.0.991250", "uap10.0.17763")]
        [InlineData("Xamarin.Forms", "4.3.0.991250", "Xamarin.iOS10")]
        [InlineData("Xamarin.Forms", "4.3.0.991250", "Xamarin.Mac20")]
        [InlineData("Xamarin.Forms", "4.3.0.991250", "netstandard2.0")]
        [InlineData("Xamarin.Essentials", "1.0.0", "MonoAndroid10.0")]
        [InlineData("Xamarin.Essentials", "1.0.0", "MonoAndroid90")]
        [InlineData("Xamarin.Essentials", "1.0.0", "uap10.0.17763")]
        [InlineData("Xamarin.Essentials", "1.0.0", "Xamarin.iOS10")]
        [InlineData("Xamarin.Essentials", "1.0.0", "Xamarin.Mac20")]
        [InlineData("Xamarin.Essentials", "1.0.0", "netstandard2.0")]
        [InlineData("Tizen.NET.API4", "4.0.1.14152 ", "netstandard2.0")]
        [InlineData("Avalonia", "0.9.12", "netstandard2.0")]
        [InlineData("Avalonia", "0.9.12", "netcoreapp3.1")]
        [InlineData("Avalonia", "0.9.12", "net5.0")]
        [InlineData("Avalonia", "0.9.12", "net472")]
        [InlineData("Avalonia.Remote.Protocol", "0.9.12", "netstandard2.0")]
        [InlineData("Uno.UI", "1.44.1", "netstandard2.0")]
        [InlineData("Uno.UI", "1.44.1", "MonoAndroid80")]
        [InlineData("Uno.UI", "1.44.1", "MonoAndroid90")]
        [InlineData("Uno.UI", "1.44.1", "Xamarin.iOS10")]
        [InlineData("Uno.UI", "1.44.1", "net5.0")]
        [InlineData("Uno.Core", "1.27.0", "netstandard2.0")]
        [InlineData("Uno.Core", "1.27.0", "net472")]
        [InlineData("Uno.Core", "1.27.0", "uap10.0.17763")]
        [InlineData("Uno.Core", "1.27.0", "net5.0")]
        public Task ProcessPackageIdentity(string packageName, string nugetVersion, string frameworkString)
        {
            var package = new[] { new PackageIdentity(packageName, new NuGetVersion(nugetVersion)) };

            return IntegrationTestHelper.CheckResultsAgainstTemplate(package, frameworkString.ToFrameworks());
        }
    }
}
