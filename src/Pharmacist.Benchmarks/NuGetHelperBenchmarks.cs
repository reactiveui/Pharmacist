using System.IO;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

namespace Pharmacist.Benchmarks
{
    [ClrJob]
    ////[CoreJob]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class NuGetTaskGeneratorBenchmarks
    {
        private static readonly string _packageDirectory = Path.Combine(Path.GetTempPath(), "Pharmacist.Benchamarks");

        [IterationSetup]
        public void IterationSetup()
        {
            try
            {
                Directory.Delete(_packageDirectory);
            }
            catch
            {
            }
        }

        [Benchmark]
        public async Task MultipleDirectoryCase()
        {
            // NetCore contains multiple directories that match.
            var package = new[] { new PackageIdentity("Microsoft.NETCore.App", new NuGetVersion("2.0.0")) };
            var frameworks = new[] { FrameworkConstants.CommonFrameworks.NetCoreApp20 };

            var result = (await NuGetPackageHelper
                              .DownloadPackageFilesAndFolder(package, frameworks, packageOutputDirectory: _packageDirectory)
                              .ConfigureAwait(false));
        }
    }
}
