#addin "nuget:?package=Cake.Coverlet&version=2.2.1"
#load "nuget:https://www.myget.org/F/wormie-nugets/api/v3/index.json?package=Cake.Recipe&prerelease"

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
    shouldRunGitVersion: true
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

((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Actions.Clear();
((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Criterias.Clear();
((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Dependencies.Clear();

BuildParameters.Tasks.DotNetCoreTestTask
    .IsDependentOn("Install-ReportGenerator")
    .Does(() => {
    var projects = GetFiles(BuildParameters.TestDirectoryPath + (BuildParameters.TestFilePattern ?? "/**/*Tests.csproj"));
    var testFileName = BuildParameters.Paths.Files.TestCoverageOutputFilePath.GetFilename();
    var testDirectory = BuildParameters.Paths.Files.TestCoverageOutputFilePath.GetDirectory();

    var settings = new CoverletSettings {
        CollectCoverage = true,
        CoverletOutputFormat = CoverletOutputFormat.opencover,
        CoverletOutputDirectory = testDirectory,
        CoverletOutputName = testFileName.ToString(),
        MergeWithFile = BuildParameters.Paths.Files.TestCoverageOutputFilePath
    };
    foreach (var line in ToolSettings.TestCoverageExcludeByFile.Split(';')) {
        foreach (var file in GetFiles("**/" + line)) {
            settings = settings.WithFileExclusion(file.FullPath);
        }
    }

    foreach (var item in ToolSettings.TestCoverageFilter.Split(' ')) {
        if (item[0] == '+') {
            settings.WithInclusion(item.TrimStart('+'));
        }
        else if (item[0] == '-') {
            settings.WithFilter(item.TrimStart('-'));
        }
    }

    var testSettings = new DotNetCoreTestSettings {
        Configuration = BuildParameters.Configuration,
        NoBuild = true
    };

    foreach (var project in projects) {
        DotNetCoreTest(project.FullPath, testSettings, settings);
    }

    if (FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath)) {
        ReportGenerator(BuildParameters.Paths.Files.TestCoverageOutputFilePath, BuildParameters.Paths.Directories.TestCoverage);
    }
});

BuildParameters.Tasks.TransifexPushSourceResource.WithCriteria(() => BuildParameters.IsRunningOnWindows);

Task("Linux")
    .IsDependentOn("Package")
    .IsDependentOn("Upload-Coverage-Report");

Task("Appveyor-Linux")
    .IsDependentOn("Linux")
    .IsDependentOn("Upload-AppVeyor-Artifacts");

BuildParameters.PrintParameters(Context);

Build.RunDotNetCore();
