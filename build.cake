#addin "Cake.Figlet"
//#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Npx&version=1.6.0"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var projectName = "Lumpy.Lib.Common";
var releaseVersion = "0.0.0";
var artifactsDir =  Directory("../../artifacts");


///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    Information(Figlet(projectName));
    
    releaseVersion = EnvironmentVariable<string>("NEW_RELEASE_VERSION", releaseVersion);
    Information("Version: {0}", releaseVersion);
});

Teardown(context =>
{
    Information("Finished running tasks ✔");
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

Task("Build")
    .IsDependentOn("Run dotnet --info")
    .IsDependentOn("Clean")
    //.IsDependentOn("Get next semantic version number")
    .IsDependentOn("Build solution")
    //.IsDependentOn("Run tests")
    .IsDependentOn("Package")
    ;

Task("Run dotnet --info")
    .Does(() =>
{
    Information("dotnet --info");
    StartProcess("dotnet", new ProcessSettings { Arguments = "--info" });
});

Task("Clean")
    .Does(() => {
    Information("Cleaning {0}, bin and obj folders", artifactsDir);
    CleanDirectory(artifactsDir);
    });

Task("Build solution")
    .Does(() => {
    var solutions = GetFiles("./*.sln");
    foreach(var solution in solutions)
    {
        Information("Building solution {0} v{1}", solution.GetFilenameWithoutExtension(), releaseVersion);

        var assemblyVersion = $"{releaseVersion}.0";
        DotNetCoreRestore(solution.FullPath);
        DotNetCoreBuild(solution.FullPath, new DotNetCoreBuildSettings()
        {
            Configuration = configuration,
            MSBuildSettings = new DotNetCoreMSBuildSettings()
                .WithProperty("Version", assemblyVersion)
                .WithProperty("AssemblyVersion", assemblyVersion)
                .WithProperty("FileVersion", assemblyVersion)
                // 0 = use as many processes as there are available CPUs to build the project
                // see: https://develop.cakebuild.net/api/Cake.Common.Tools.MSBuild/MSBuildSettings/60E763EA
                .SetMaxCpuCount(0)
        });
    }
});

// Task("Run tests")
//     .Does(() =>
// {
//     var xunitArgs = "-nobuild -configuration " + configuration;

//     var testProjects = GetFiles("./src/**/*.Tests.csproj");
//     foreach(var testProject in testProjects)
//     {
//         Information("Testing project {0} with args {1}", testProject.GetFilenameWithoutExtension(), xunitArgs);

//         DotNetCoreTool(testProject.FullPath, "xunit", xunitArgs);
//     }
// });

Task("Package")
    .Does(() =>
{
    var projects = GetFiles("./**/*.csproj");
    foreach(var project in projects)
    {
        var projectDirectory = project.GetDirectory().FullPath;
        if(projectDirectory.EndsWith("Tests")) continue;

        Information("Packaging project {0} v{1}", project.GetFilenameWithoutExtension(), releaseVersion);

        var assemblyVersion = $"{releaseVersion}.0";

        DotNetCorePack(project.FullPath, new DotNetCorePackSettings {
            Configuration = configuration,
            OutputDirectory = artifactsDir,
            NoBuild = true,
            MSBuildSettings = new DotNetCoreMSBuildSettings()
                .WithProperty("Version", assemblyVersion)
                .WithProperty("AssemblyVersion", assemblyVersion)
                .WithProperty("FileVersion", assemblyVersion)
        });
    }
});


///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);