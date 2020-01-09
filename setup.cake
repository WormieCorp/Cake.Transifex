#load "nuget:https://f.feedz.io/wormiecorp/packages/nuget/index.json?package=Cake.Recipe&version=2.0.0-alpha0036&prerelease"

Environment.SetVariableNames();

BuildParameters.SetParameters(
    context: Context,
    buildSystem: BuildSystem,
    sourceDirectoryPath: "./src",
    title: "Cake.Transifex",
    repositoryOwner: "cake-contrib",
    repositoryName: "Cake.Transifex",
    appVeyorAccountName: "cakecontrib",
    shouldRunDotNetCorePack: true,
    shouldBuildNugetSourcePackage: false,
    shouldDeployGraphDocumentation: false,
    solutionFilePath: "./Cake.Transifex.sln",
    testFilePattern: "/**/*.Tests.csproj",
    shouldRunCodecov: true,
    shouldExecuteGitLink: false,
    shouldRunGitVersion: true,
    shouldRunDupFinder: false
);

ToolSettings.SetToolSettings(
    context: Context,
    dupFinderExcludePattern: new string[] {
        BuildParameters.RootDirectoryPath + "/src/*.Tests/**/*.cs"
    },
    dupFinderExcludeFilesByStartingCommentSubstring: new string[] {
        "<auto-generated>"
    },
    testCoverageFilter: "+[Cake.Transifex*]* -[*.Tests]*",
    testCoverageExcludeByAttribute: "*.ExcludeFromCodeCoverage*",
    testCoverageExcludeByFile: "*Designer.cs;*.g.cs;*.g.i.cs"
);

if (BuildParameters.IsRunningOnAppVeyor &&
    BuildParameters.IsMainRepository && BuildParameters.IsMasterBranch && !BuildParameters.IsTagged) {
    BuildParameters.Tasks.AppVeyorTask.IsDependentOn("Create-Release-Notes");
}

BuildParameters.Tasks.TransifexPushSourceResource.WithCriteria(() => BuildParameters.IsRunningOnWindows);

Task("Linux")
    .IsDependentOn("Package")
    .IsDependentOn("Upload-Coverage-Report");

Task("Appveyor-Linux")
    .IsDependentOn("Linux")
    .IsDependentOn("Upload-AppVeyor-Artifacts");

BuildParameters.PrintParameters(Context);

Build.RunDotNetCore();
