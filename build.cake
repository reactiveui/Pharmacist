#load nuget:https://www.myget.org/F/reactiveui/api/v2?package=ReactiveUI.Cake.Recipe&prerelease

Environment.SetVariableNames();

// Whitelisted Packages
var packageWhitelist = new[] 
{ 
    MakeAbsolute(File("./src/Pharmacist.Console/Pharmacist.Console.csproj")),
    MakeAbsolute(File("./src/Pharmacist.Core/Pharmacist.Core.csproj")),
    //MakeAbsolute(File("./src/Pharmacist.MSBuild/Pharmacist.MSBuild.csproj")),
    MakeAbsolute(File("./src/Pharmacist.Common/Pharmacist.Common.csproj")),
};

var packageTestWhitelist = new[]
{
    MakeAbsolute(File("./src/Pharmacist.Tests/Pharmacist.Tests.csproj")),
};

var msbuildTask = Task("BuildMsBuild")
    .IsDependentOn("GitVersion")
    .Does(() =>
{
    var msBuildSettings = new MSBuildSettings() {
            Restore = true,
            ToolPath = ToolSettings.MsBuildPath,
        }
        .WithProperty("TreatWarningsAsErrors", BuildParameters.TreatWarningsAsErrors.ToString())
        .SetMaxCpuCount(ToolSettings.MaxCpuCount)
        .SetConfiguration(BuildParameters.Configuration)
        .WithTarget("build")
        .SetVerbosity(Verbosity.Minimal);

    if (doNotOptimise)
    {
        msBuildSettings = msBuildSettings.WithProperty("Optimize",  "False");
    }

    MSBuild(projectPath, msBuildSettings);   
});

BuildParameters.Tasks.TestxUnitCoverletGenerateTask.IsDependentOn(msbuildTask);

BuildParameters.SetParameters(context: Context, 
                            buildSystem: BuildSystem,
                            title: "Pharmacist",
                            whitelistPackages: packageWhitelist,
                            whitelistTestPackages: packageTestWhitelist,
                            artifactsDirectory: "./artifacts",
                            sourceDirectory: "./src");

ToolSettings.SetToolSettings(context: Context);

Build.Run();
