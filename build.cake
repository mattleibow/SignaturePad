#tool nuget:?package=XamarinComponent

#addin nuget:?package=Cake.Xamarin
#addin nuget:?package=Cake.FileHelpers

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
DirectoryPath outDir = "./output/";
FilePath XamarinComponentPath = "./tools/XamarinComponent/tools/xamarin-component.exe";

var Build = new Action<string>((solution) =>
{
    if (IsRunningOnWindows()) {
        MSBuild(solution, s => s.SetConfiguration(configuration).SetMSBuildPlatform(MSBuildPlatform.x86));
    } else {
        XBuild(solution, s => s.SetConfiguration(configuration));
    }
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    var dirs = new [] { 
        "./output",
        "./src/*/bin", 
        "./src/*/obj", 
        "./samples/*/packages",
        "./samples/*/bin",
        "./samples/*/obj",
    };
    foreach (var dir in dirs) {
        Information("Cleaning {0}...", dir);
        CleanDirectories(dir);
    }
});

Task("RestorePackages")
    .Does(() =>
{
    var solutions = new [] { 
        "./src/SignaturePad.sln", 
        "./samples/Sample.Android/Sample.Android.sln",
        "./samples/Sample.iOS/Sample.iOS.sln",
        "./samples/Sample.WP7/Sample.WP7.sln",
    };
    foreach (var solution in solutions) {
        Information("Restoring {0}...", solution);
        NuGetRestore(solution, new NuGetRestoreSettings {
            Source = new [] { IsRunningOnWindows() ? "https://api.nuget.org/v3/index.json" : "https://www.nuget.org/api/v2/" },
            Verbosity = NuGetVerbosity.Detailed
        });
    }
});

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    // build
    Build("./src/SignaturePad.sln");
    
    // copy outputs
    var outputs = new Dictionary<string, string>
    {
        { "./src/SignaturePad.Android/bin/{0}/SignaturePad.Android.dll", "android/SignaturePad.Android.dll" },
        { "./src/SignaturePad.iOS/bin/classic/{0}/SignaturePad.iOS.dll", "ios/SignaturePad.iOS.dll" },
        { "./src/SignaturePad.iOS/bin/unified/{0}/SignaturePad.iOS.dll", "ios-unified/SignaturePad.iOS.dll" },
        { "./src/SignaturePad.WP7/bin/{0}/SignaturePad.WP7.dll", "wp8/SignaturePad.WP7.dll" },
    };
    foreach (var output in outputs) {
        var dest = outDir.CombineWithFilePath(string.Format(output.Value, configuration));
        var dir = dest.GetDirectory();
        if (!DirectoryExists(dir)) {
            CreateDirectory(dir);
        }
        CopyFile(string.Format(output.Key, configuration), dest);
    }
});

Task("Samples")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    // var solutions = new List<string> { 
    //     ForEverywhere ? "./Demos/Microsoft.Band.Sample/Microsoft.Band.Sample.sln" : (ForWindowsOnly ? "./Demos/Microsoft.Band.Sample/Microsoft.Band.Android.Sample.sln" : "./Demos/Microsoft.Band.Sample/Microsoft.Band.iOS.Sample.sln"),
    //     ForEverywhere ? "./Demos/Microsoft.Band.Portable.Sample/Microsoft.Band.Portable.Sample.sln" : (ForWindowsOnly ? "./Demos/Microsoft.Band.Portable.Sample/Microsoft.Band.Portable.Sample.Windows.sln" : "./Demos/Microsoft.Band.Portable.Sample/Microsoft.Band.Portable.Sample.Mac.sln"),
    // };
    // if (ForWindows) {
    //     solutions.Add("./Demos/RotatingHand/RotatingHand.sln");
    // }
    // foreach (var solution in solutions) {
    //     Information("Building {0}...", solution);
    //     Build(solution);
    // }
});

Task("Package")
    .IsDependentOn("Build")
    .Does(() =>
{
    // NuGet
    Information("Packing NuGet...");
    NuGetPack("./nuget/Xamarin.Controls.SignaturePad.nuspec", new NuGetPackSettings {
        OutputDirectory = outDir,
        Verbosity = NuGetVerbosity.Detailed,
        BasePath = IsRunningOnUnix() ? "././" : "./",
    });
    
    // Xamarin Component
    Information("Packing Component...");
    DeleteFiles("./component/*.xam");
    PackageComponent("./component/", new XamarinComponentSettings {
        ToolPath = XamarinComponentPath
    });
    DeleteFiles("./output/*.xam");
    MoveFiles("./component/*.xam", outDir);
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    // NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
    //     NoResults = true
    //     });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Package")
    .IsDependentOn("Samples")
    .IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);