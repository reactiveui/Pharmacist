// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

using Pharmacist.Core;
using Pharmacist.Core.NuGet;

using Shouldly;

namespace Pharmacist.Tests.IntegrationTests
{
    internal static class IntegrationTestHelper
    {
        private static readonly Regex _whitespaceRegex = new Regex(@"\s");

        public static async Task CheckResultsAgainstTemplate(PackageIdentity[] package, IReadOnlyCollection<NuGetFramework> frameworks, [CallerFilePath]string filePath = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(memoryStream, package, frameworks).ConfigureAwait(false);
                CheckContents(memoryStream, package[0], filePath);
            }
        }

        public static async Task CheckResultsAgainstTemplate(LibraryRange[] package, IReadOnlyCollection<NuGetFramework> frameworks, [CallerFilePath]string filePath = null)
        {
            var bestPackageIdentity = await NuGetPackageHelper.GetBestMatch(package[0], new SourceRepository(new PackageSource(NuGetPackageHelper.DefaultNuGetSource), NuGetPackageHelper.Providers), CancellationToken.None).ConfigureAwait(false);

            using (var memoryStream = new MemoryStream())
            {
                await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(memoryStream, package, frameworks).ConfigureAwait(false);
                CheckContents(memoryStream, bestPackageIdentity, filePath);
            }
        }

        private static void CheckContents(MemoryStream memoryStream, PackageIdentity bestPackageIdentity, string filePath)
        {
            var sourceDirectory = Path.GetDirectoryName(filePath);

            var approvedFileName = Path.Combine(sourceDirectory, $"{bestPackageIdentity.Id}.{bestPackageIdentity.Version}.approved.txt");
            var receivedFileName = Path.Combine(sourceDirectory, $"{bestPackageIdentity.Id}.{bestPackageIdentity.Version}.received.txt");

            if (!File.Exists(approvedFileName))
            {
                File.Create(approvedFileName);
            }

            if (!File.Exists(receivedFileName))
            {
                File.Create(receivedFileName);
            }

            memoryStream.Flush();

            memoryStream.Position = 0;
            using (var sr = new StreamReader(memoryStream))
            {
                var actualContents = sr.ReadToEnd().Trim('\n').Trim('\r');
                var expectedContents = File.ReadAllText(approvedFileName);

                string normalizedActual = _whitespaceRegex.Replace(actualContents, string.Empty);
                string normalizedExpected = _whitespaceRegex.Replace(expectedContents, string.Empty);

                if (!string.Equals(normalizedActual, normalizedExpected, StringComparison.InvariantCulture))
                {
                    File.WriteAllText(receivedFileName, actualContents);
                    ShouldlyConfiguration.DiffTools.GetDiffTool().Open(receivedFileName, approvedFileName, true);
                }

                normalizedActual.ShouldNotBeEmpty();

                normalizedActual.ShouldBe(normalizedExpected, StringCompareShould.IgnoreLineEndings);
            }
        }
    }
}
