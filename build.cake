#addin nuget:?package=Cake.Git&version=3.0.0
#addin nuget:?package=Cake.Coverlet&version=3.0.4
#addin nuget:?package=Cake.Compression&version=0.3.0

#tool dotnet:?package=GitVersion.Tool&version=5.10.3
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.1.10

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;

using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Reflection.Metadata;

var version = string.Empty;
var artifactsDir = "./artifacts/";
var target = Argument("target", "PublishGithub");
var project = "./src/RabbitMq.Connector/RabbitMq.Connector.csproj";
var nugetSource = "https://api.nuget.org/v3/index.json";
var configuration = Argument("configuration", "Release");

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    CleanDirectory($"./RabbitMq.Connector/bin/{configuration}");
});

Task("Version")
   .IsDependentOn("Clean")
   .Does(() =>
   {
       var result = GitVersion(new GitVersionSettings
       {
           UpdateAssemblyInfo = true,
           ConfigFile = new FilePath("./GitVersion.yml")
       });

       version = result.NuGetVersionV2.Split('-')[0];
       Information($"Version: {version}");
   });

Task("Build")
    .IsDependentOn("Version")
    .Does(() =>
    {
        var buildSettings = new DotNetBuildSettings
        {
            Configuration = configuration,
            MSBuildSettings = new DotNetMSBuildSettings()
                                                         .WithProperty("Version", version)
                                                         .WithProperty("AssemblyVersion", version)
                                                         .WithProperty("FileVersion", version)
        };
        var projects = GetFiles("./**/**/*.csproj");
        foreach (var project in projects)
        {
            Information($"Building {project.ToString()}");
            DotNetBuild(project.ToString(), buildSettings);
        }
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {

        Information("Checking coverage");
        if (System.IO.File.Exists("result.cobertura.xml"))
            System.IO.File.Delete("result.cobertura.xml");

        if (System.IO.Directory.Exists("coverageOutput"))
            System.IO.Directory.Delete("coverageOutput", true);

        var testSetting = new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
            Verbosity = DotNetVerbosity.Minimal,
            Filter = "Kind!=E2E",
        };

        var coverletSetting = new CoverletSettings
        {
            CollectCoverage = true,
            CoverletOutputFormat = CoverletOutputFormat.cobertura,
            CoverletOutputDirectory = new DirectoryPath(@"./coverageOutput"),
            CoverletOutputName = "result.cobertura.xml"
        };

        coverletSetting.WithFilter("[RabbitMq.Connector*]*Exceptions*")
                       .WithFilter("[RabbitMq.Connector*]*Extensions*")
                       .WithFilter("[RabbitMq.Connector*]*Model*")
                       .WithFilter("[RabbitMq.Connector*]*IoC*");

        DotNetTest("./RabbitMq.Connect.sln", testSetting, coverletSetting);
    });

Task("CheckCoverage")
    .IsDependentOn("Test")
    .Does(() =>
    {
        Information("Running Report Generator...");
        var pathFullXml = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory().ToString(), "coverageOutput/result.cobertura.xml");
        var pathFullTxt = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory().ToString(), "coverageOutput/Summary.txt");

        var ps = new ProcessSettings
        {
            Arguments = $"\"-reports:./coverageOutput/result.cobertura.xml\" \"-targetdir:coverageOutput\" \"-reporttypes:Html;TextSummary\"",
            RedirectStandardOutput = false
        };

        ReportGenerator(new FilePath(pathFullXml), new DirectoryPath("./coverageOutput/"), new ReportGeneratorSettings
        {
            ReportTypes = new[]
        {
            ReportGeneratorReportType.TextSummary,
            ReportGeneratorReportType.Html,
        }
        });

        var summary = System.IO.File.ReadAllText($"{pathFullTxt}");
        const string patten = @"((Coverable lines|Covered lines|Line coverage): (?<value>\d+(.\d)?)%?)";

        var matches = Regex.Matches(summary, patten);
        var linePercent = matches[0].Groups["value"].Value;
        var linesCovered = matches[1].Groups["value"].Value;
        var totalLines = matches[2].Groups["value"].Value; Information($"Covered Lines Percentage: {linePercent}%");

        Information($"Total Covered Lines: {linesCovered}");
        Information($"Total Lines: {totalLines}");
        if (double.TryParse(linePercent, out double linePercentValue))
        {
            if (linePercentValue < 94)
            {
                Warning($"The coverage percentage is under 94%");
            }
        }
    });

Task("Pack")
  .IsDependentOn("CheckCoverage")
  .Does(() =>
  {
      var settings = new DotNetPackSettings
      {
          Configuration = configuration,
          OutputDirectory = artifactsDir,
          NoBuild = true,
          NoRestore = true,
          MSBuildSettings = new DotNetMSBuildSettings()
                           .WithProperty("PackageVersion", version)
                           .WithProperty("Copyright", $"Copyright José Almir - {DateTime.Now.Year}")
                           .WithProperty("Version", version)
      };

      DotNetPack("./RabbitMq.Connect.sln", settings);
  });

Task("PublishNuget")
 .IsDependentOn("Pack")
 .Does(context =>
 {
    Information("PublishNuget: {0}", BuildSystem.GitHubActions.IsRunningOnGitHubActions);
    var fullPathArtifact = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory().ToString(), "/artifacts/*.nupkg");

    Information(fullPathArtifact);
    if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
    {
         foreach (var file in GetFiles("./**/artifacts/*.nupkg"))
         {
            Information("Publishing {0}...", file.GetFilename().FullPath);
            DotNetNuGetPush(file, new DotNetNuGetPushSettings
            {
                ApiKey = context.EnvironmentVariable("NUGET_API_KEY"),
                
                Source = "https://api.nuget.org/v3/index.json"
            });
         }
    }
 });

Task("PublishGithub")
 .IsDependentOn("PublishNuget")
 .Does(context =>
 {
    Information("PublishGithub: {0 }", BuildSystem.GitHubActions.IsRunningOnGitHubActions);
    var fullPathArtifact = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory().ToString(), "/artifacts/*.nupkg");
    if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
    {
        foreach (var file in GetFiles(fullPathArtifact))
        {
            Information("Publishing {0}...", file.GetFilename().FullPath);
            DotNetNuGetPush(file, new DotNetNuGetPushSettings
            {
                ApiKey = EnvironmentVariable("GITHUB_TOKEN"),
                Source = "https://nuget.pkg.github.com/threenine/index.json"
            });
        }
    }
 });

private void DeleteLinesFromFile(string pathFullTxt, string strLineToDelete)
{
    string strSearchText = strLineToDelete;
    string strOldText;
    string n = "";

    StreamReader sr = System.IO.File.OpenText(pathFullTxt);
    while ((strOldText = sr.ReadLine()) != null)
    {
        if (!strOldText.Contains(strSearchText))
        {
            n += strOldText + Environment.NewLine;
        }
    }
    sr.Close();
    System.IO.File.WriteAllText(pathFullTxt, n);
}

RunTarget(target);
