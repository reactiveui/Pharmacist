// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

namespace Pharmacist.Core.ReferenceLocators
{
    /// <summary>
    /// Implements the reference locations for windows builds.
    /// </summary>
    public static class ReferenceLocator
    {
        private static readonly PackageIdentity VSWherePackageIdentity = new PackageIdentity("VSWhere", new NuGetVersion("2.6.7"));

        /// <summary>
        /// Gets the reference location.
        /// </summary>
        /// <param name="includePreRelease">If we should include pre-release software.</param>
        /// <returns>The reference location.</returns>
        public static Task<string> GetReferenceLocation(bool includePreRelease = true)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Task.FromResult("/Library⁩/Frameworks⁩/Libraries/⁨mono");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsInstallationDirectory(includePreRelease);
            }

            throw new ReferenceLocationNotFoundException("Visual Studio reference location not supported on this platform: " + RuntimeInformation.OSDescription);
        }

        private static async Task<string> GetWindowsInstallationDirectory(bool includePreRelease)
        {
            var results = await NuGetPackageHelper.DownloadPackageFilesAndFolder(
                              new[] { VSWherePackageIdentity },
                              new[] { new NuGetFramework("Any") },
                              packageFolders: new[] { PackagingConstants.Folders.Tools },
                              getDependencies: false).ConfigureAwait(false);

            var fileName = results.SelectMany(x => x.files).FirstOrDefault(x => x.EndsWith("vswhere.exe", StringComparison.InvariantCultureIgnoreCase));

            if (fileName == null)
            {
                throw new ReferenceLocationNotFoundException("Cannot find visual studio installation, due to vswhere not being installed correctly.");
            }

            var parameters = new StringBuilder("-latest -nologo -property installationPath -format value");

            if (includePreRelease)
            {
                parameters.Append(" -prerelease");
            }

            return await Task.Run(() =>
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = parameters.ToString();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;

                    process.Start();

                    // To avoid deadlocks, always read the output stream first and then wait.
                    string output = process.StandardOutput.ReadToEnd().Replace(Environment.NewLine, string.Empty);
                    process.WaitForExit();

                    return Path.Combine(output, "Common7", "IDE", "ReferenceAssemblies", "Microsoft", "Framework");
                }
            }).ConfigureAwait(false);
        }
    }
}
