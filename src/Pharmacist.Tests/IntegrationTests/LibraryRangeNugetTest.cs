// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using NuGet.LibraryModel;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

using Xunit;

namespace Pharmacist.Tests.IntegrationTests
{
    public class LibraryRangeNuGetTest
    {
        [Theory]
        [InlineData("Xamarin.Forms", "4.*", "MonoAndroid81")]
        [InlineData("Xamarin.Forms", "4.*", "MonoAndroid90")]
        [InlineData("Xamarin.Forms", "4.*", "tizen40")]
        [InlineData("Xamarin.Forms", "4.*", "uap10.0.17763")]
        [InlineData("Xamarin.Forms", "4.*", "Xamarin.iOS10")]
        [InlineData("Xamarin.Forms", "4.*", "Xamarin.Mac20")]
        [InlineData("Xamarin.Forms", "4.*", "netstandard2.0")]
        [InlineData("Xamarin.Essentials", "1.*", "MonoAndroid81")]
        [InlineData("Xamarin.Essentials", "1.*", "MonoAndroid90")]
        [InlineData("Xamarin.Essentials", "1.*", "uap10.0.17763")]
        [InlineData("Xamarin.Essentials", "1.*", "Xamarin.iOS10")]
        [InlineData("Xamarin.Essentials", "1.*", "Xamarin.Mac20")]
        [InlineData("Xamarin.Essentials", "1.*", "netstandard2.0")]
        [InlineData("Tizen.NET.API4", "4.*", "netstandard2.0")]
        [InlineData("Avalonia", "0.9.*", "netstandard2.0")]
        [InlineData("Avalonia", "0.9.*", "netcoreapp2.0")]
        [InlineData("Avalonia", "0.9.*", "net461")]
        [InlineData("Avalonia.Remote.Protocol", "0.9.*", "netstandard2.0")]
        [InlineData("Uno.UI", "1.44.*", "netstandard2.0")]
        [InlineData("Uno.UI", "1.44.*", "MonoAndroid80")]
        [InlineData("Uno.UI", "1.44.*", "MonoAndroid90")]
        [InlineData("Uno.UI", "1.44.*", "Xamarin.iOS10")]
        [InlineData("Uno.Core", "1.27.*", "netstandard2.0")]
        [InlineData("Uno.Core", "1.27.*", "net461")]
        [InlineData("Uno.Core", "1.27.*", "uap10.0.17763")]
        public Task ProcessLibraryRange(string packageName, string nugetVersion, string framework)
        {
            var package = new[] { new LibraryRange(packageName, VersionRange.Parse(nugetVersion), LibraryDependencyTarget.Package) };

            return IntegrationTestHelper.CheckResultsAgainstTemplate(package, framework.ToFrameworks());
        }
    }
}
