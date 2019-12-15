#load nuget:https://pkgs.dev.azure.com/dotnet/ReactiveUI/_packaging/ReactiveUI/nuget/v3/index.json?package=ReactiveUI.Cake.Recipe&prerelease

Environment.SetVariableNames();

// Whitelisted Packages
var packageWhitelist = new[] 
{ 
    MakeAbsolute(File("./src/Pharmacist.Console/Pharmacist.Console.csproj")),
    MakeAbsolute(File("./src/Pharmacist.Core/Pharmacist.Core.csproj")),
    MakeAbsolute(File("./src/Pharmacist.MSBuild.NuGet/Pharmacist.MSBuild.NuGet.csproj")),
    MakeAbsolute(File("./src/Pharmacist.MSBuild.TargetFramework/Pharmacist.MSBuild.TargetFramework.csproj")),
    MakeAbsolute(File("./src/Pharmacist.Common/Pharmacist.Common.csproj")),
};

var packageTestWhitelist = new[]
{
    MakeAbsolute(File("./src/Pharmacist.Tests/Pharmacist.Tests.csproj")),
};

BuildParameters.SetParameters(context: Context, 
                            buildSystem: BuildSystem,
                            title: "Pharmacist",
                            whitelistPackages: packageWhitelist,
                            whitelistTestPackages: packageTestWhitelist,
                            artifactsDirectory: "./artifacts",
                            sourceDirectory: "./src");

ToolSettings.SetToolSettings(context: Context, maxCpuCount: 1);

Build.Run();
