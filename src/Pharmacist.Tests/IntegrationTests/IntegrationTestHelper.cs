// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        public static async Task CheckResultsAgainstTemplate(PackageIdentity[] package, IReadOnlyList<NuGetFramework> frameworks, [CallerFilePath]string filePath = null)
        {
            using (var memoryStream = new MemoryStream())
            {
                await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(memoryStream, package, frameworks, TestUtilities.PackageDirectory).ConfigureAwait(false);
                CheckPackageIdentityContents(memoryStream, package[0], frameworks[0], filePath);
            }
        }

        public static async Task CheckResultsAgainstTemplate(LibraryRange[] package, IReadOnlyList<NuGetFramework> frameworks, [CallerFilePath]string filePath = null)
        {
            var bestPackageIdentity = await NuGetPackageHelper.GetBestMatch(package[0], new SourceRepository(new PackageSource(NuGetPackageHelper.DefaultNuGetSource), NuGetPackageHelper.Providers), CancellationToken.None).ConfigureAwait(false);

            using (var memoryStream = new MemoryStream())
            {
                await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(memoryStream, package, frameworks, TestUtilities.PackageDirectory).ConfigureAwait(false);
                CheckPackageIdentityContents(memoryStream, bestPackageIdentity, frameworks[0], filePath);
            }
        }

        public static void CheckContents(string actualContents, string approvedFileName, string receivedFileName)
        {
            if (!File.Exists(approvedFileName))
            {
                File.Create(approvedFileName).Close();
            }

            if (!File.Exists(receivedFileName))
            {
                File.Create(receivedFileName).Close();
            }

            var expectedContents = File.ReadAllText(approvedFileName);

            string normalizedActual = _whitespaceRegex.Replace(actualContents, string.Empty);
            string normalizedExpected = _whitespaceRegex.Replace(expectedContents, string.Empty);

            if (!string.Equals(normalizedActual, normalizedExpected, StringComparison.InvariantCulture))
            {
                File.WriteAllText(receivedFileName, actualContents);
                try
                {
                    ShouldlyConfiguration.DiffTools.GetDiffTool().Open(receivedFileName, approvedFileName, true);
                }
                catch (ShouldAssertException)
                {
                    var process = new Process
                                  {
                                      StartInfo = new ProcessStartInfo
                                                  {
                                                      Arguments = $"\"{approvedFileName}\" \"{receivedFileName}\"",
                                                      UseShellExecute = false,
                                                      RedirectStandardOutput = true,
                                                      CreateNoWindow = true
                                                  }
                                  };

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        process.StartInfo.FileName = "FC";
                    }
                    else
                    {
                        process.StartInfo.FileName = "diff";
                    }

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    throw new Exception("Invalid API configuration: " + Environment.NewLine + output);
                }
            }

            normalizedActual.ShouldNotBeEmpty();

            normalizedActual.ShouldBe(normalizedExpected, StringCompareShould.IgnoreLineEndings);
        }

        public static string GetOutputDirectory([CallerFilePath] string filePath = null) => Path.Combine(Path.GetDirectoryName(filePath), "Approved");

        private static void CheckPackageIdentityContents(MemoryStream memoryStream, PackageIdentity bestPackageIdentity, NuGetFramework nugetFramework, string filePath)
        {
            var sourceDirectory = GetOutputDirectory(filePath);

            var approvedFileName = Path.Combine(sourceDirectory, $"{bestPackageIdentity.Id}.{bestPackageIdentity.Version}.{nugetFramework.GetShortFolderName()}.approved.txt");
            var receivedFileName = Path.Combine(sourceDirectory, $"{bestPackageIdentity.Id}.{bestPackageIdentity.Version}.{nugetFramework.GetShortFolderName()}.received.txt");

            memoryStream.Flush();
            memoryStream.Position = 0;
            using (var sr = new StreamReader(memoryStream))
            {
                CheckContents(sr.ReadToEnd().Trim('\n').Trim('\r'), approvedFileName, receivedFileName);
            }
        }
    }
}
