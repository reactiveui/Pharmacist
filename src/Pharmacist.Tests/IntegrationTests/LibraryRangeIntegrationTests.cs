using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

using Xunit;

namespace Pharmacist.Tests.IntegrationTests
{
    public class LibraryRangeIntegrationTests
    {
        /// <summary>
        /// Tests to make sure the Tizen platform produces the expected results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public Task TizenNuGetTest()
        {
            var package = new[] { new LibraryRange("Tizen.NET.API4", VersionRange.Parse("4.0.1.*"), LibraryDependencyTarget.Package) };
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
            var package = new[] { new LibraryRange("Xamarin.Essentials", VersionRange.Parse("1.1.*"), LibraryDependencyTarget.Package) };
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
            var package = new[] { new LibraryRange("Xamarin.Forms", VersionRange.Parse("4.0.0.*"), LibraryDependencyTarget.Package) };
            var frameworks = "MonoAndroid81".ToFrameworks();

            return IntegrationTestHelper.CheckResultsAgainstTemplate(package, frameworks);
        }
    }
}
