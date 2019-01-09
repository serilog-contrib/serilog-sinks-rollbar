#tool nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.3.1

#addin nuget:?package=Cake.Sonar&version=1.1.18

var target = Argument("target", "Default");
var projectName = Argument("project", "Serilog.Sinks.RollbarCom");

var buildConfiguration = "Release";
var solutionName = "Serilog.Sinks.RollbarCom";
var solutionFileName = string.Format("./src/{0}.sln", solutionName);
var projectFolder = string.Format("./src/{0}/", projectName);
var projectFile = string.Format("{0}{1}.csproj", projectFolder, projectName);

var extensionsVersion = XmlPeek(projectFile, "Project/PropertyGroup[1]/VersionPrefix/text()");

Task("UpdateBuildVersion")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
    var buildNumber = BuildSystem.AppVeyor.Environment.Build.Number;

    BuildSystem.AppVeyor.UpdateBuildVersion(string.Format("{0}.{1}", extensionsVersion, buildNumber));
});

Task("Build")
  .Does(() =>
{
    DotNetCoreBuild(solutionFileName, new DotNetCoreBuildSettings {
        Configuration = buildConfiguration
    });
});

Task("NugetPack")
  .IsDependentOn("Build")
  .Does(() =>
{
     var settings = new DotNetCorePackSettings
     {
         Configuration = buildConfiguration,
         OutputDirectory = "."
     };

     DotNetCorePack(projectFolder, settings);
});

Task("CreateArtifact")
  .IsDependentOn("NugetPack")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
    BuildSystem.AppVeyor.UploadArtifact(string.Format("{0}.{1}.nupkg", projectName, extensionsVersion));
});

Task("SonarBegin")
  .Does(() => {
     SonarBegin(new SonarBeginSettings {
        Url = "https://sonarcloud.io",
        Login = EnvironmentVariable("sonar:apikey"),
        Key = "serilog-sinks-rollbar",
        Name = "Serilog.Sinks.RollbarCom",
        ArgumentCustomization = args => args
            .Append($"/o:olsh-github"),
        Version = "1.0.0.0"
     });
  });

Task("SonarEnd")
  .Does(() => {
     SonarEnd(new SonarEndSettings {
        Login = EnvironmentVariable("sonar:apikey")
     });
  });

Task("Sonar")
  .IsDependentOn("SonarBegin")
  .IsDependentOn("Build")
  .IsDependentOn("SonarEnd");

Task("Default")
    .IsDependentOn("NugetPack");

Task("CI")
    .IsDependentOn("UpdateBuildVersion")
    .IsDependentOn("Sonar")
    .IsDependentOn("CreateArtifact");

RunTarget(target);
