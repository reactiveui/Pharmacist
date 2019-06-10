#load nuget:https://www.myget.org/F/reactiveui/api/v2?package=ReactiveUI.Cake.Recipe&prerelease

Environment.SetVariableNames();

// Whitelisted Packages
var packageWhitelist = new[] 
{ 
    MakeAbsolute(File("./src/Pharmacist.Console/Pharmacist.Console.csproj")),
    MakeAbsolute(File("./src/Pharmacist.Core/Pharmacist.Core.csproj")),
    MakeAbsolute(File("./src/Pharmacist.MSBuild/Pharmacist.MSBuild.csproj")),
    MakeAbsolute(File("./src/Pharmacist.Common/Pharmacist.Common.csproj")),
};

var packageTestWhitelist = new[]
{
    MakeAbsolute(File("./src/Pharmacist.Tests/Pharmacist.Tests.csproj")),
};

var killMsBuildTask = Task("KillMsBuild").Does(() =>
{
    var workers = System.Diagnostics.Process.GetProcessesByName("msbuild");
    foreach (var worker in workers)
    {
        worker.Kill();
        worker.WaitForExit();
        worker.Dispose();
    }
});

BuildParameters.Tasks.TestxUnitCoverletGenerateTask.IsDependentOn(killMsBuildTask);

BuildParameters.SetParameters(context: Context, 
                            buildSystem: BuildSystem,
                            title: "Pharmacist",
                            whitelistPackages: packageWhitelist,
                            whitelistTestPackages: packageTestWhitelist,
                            artifactsDirectory: "./artifacts",
                            sourceDirectory: "./src");

ToolSettings.SetToolSettings(context: Context, maxCpuCount: 1);

Build.Run();
