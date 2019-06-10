using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using Pharmacist.Core;
using Pharmacist.Core.NuGet;

using Shouldly;

using Xunit;

namespace Pharmacist.Tests
{
    public class LibraryRangeIntegrationTests
    {
        private static readonly Regex _whitespaceRegex = new Regex(@"\s");

        /// <summary>
        /// Tests to make sure the Tizen platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task TizenNuGetTest()
        {
            var package = new[] { new LibraryRange("Tizen.NET.API4", VersionRange.Parse("4.0.1.*"), LibraryDependencyTarget.Package) };
            var frameworks = new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };

            return CheckResultsAgainstTemplate(package, frameworks);
        }

        /// <summary>
        /// Tests to make sure the Xamarin.Essentials platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task XamarinEssentialsNuGetTest()
        {
            var package = new[] { new LibraryRange("Xamarin.Essentials", VersionRange.Parse("1.1.*"), LibraryDependencyTarget.Package) };
            var frameworks = "MonoAndroid81".ToFrameworks();

            return CheckResultsAgainstTemplate(package, frameworks);
        }

        /// <summary>
        /// Tests to make sure the Xamarin.Forms platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task XamarinFormsNuGetTest()
        {
            var package = new[] { new LibraryRange("Xamarin.Forms", VersionRange.Parse("4.0.0.*"), LibraryDependencyTarget.Package) };
            var frameworks = "MonoAndroid81".ToFrameworks();

            return CheckResultsAgainstTemplate(package, frameworks);
        }

        private static async Task CheckResultsAgainstTemplate(LibraryRange[] package, IReadOnlyCollection<NuGetFramework> frameworks)
        {
            var bestPackageIdentity = await NuGetPackageHelper.GetBestMatch(package[0], new SourceRepository(new PackageSource(NuGetPackageHelper.DefaultNuGetSource), NuGetPackageHelper.Providers), CancellationToken.None).ConfigureAwait(false);
            using (var memoryStream = new MemoryStream())
            {
                await ObservablesForEventGenerator.ExtractEventsFromNuGetPackages(memoryStream, package, frameworks).ConfigureAwait(false);
                memoryStream.Flush();

                memoryStream.Position = 0;
                using (var sr = new StreamReader(memoryStream))
                {
                    var actualContents = sr.ReadToEnd();
                    var expectedContents = File.ReadAllText($"TestExpectedResults/{package[0].Name}.{bestPackageIdentity.Version}.txt");

                    string normalizedActual = _whitespaceRegex.Replace(actualContents, string.Empty);
                    string normalizedExpected = _whitespaceRegex.Replace(expectedContents, string.Empty);

                    normalizedActual.ShouldNotBeEmpty();

                    normalizedActual.ShouldBe(normalizedExpected, StringCompareShould.IgnoreLineEndings);
                }
            }
        }
    }
}
