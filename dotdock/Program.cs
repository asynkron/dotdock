using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using net.r_eg.MvsSln;

namespace dotdock
{
    internal partial class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        private static void RunOptions(Options opts)
        {
            //TODO: copy default .dockerignore file to output dir
            //TODO: allow specifying ports to expose
            //TODO: allow specifying additional startup args
            //TODO: allow selecting what base images to use
            //TODO: filter out projects that are not apps?

            var template = @"
FROM %BUILDIMAGE% AS build-env
WORKDIR /app

# Copy nugetconfigs 
%COPYNUGET%
# Copy csprojs 
%COPYCSPROJ%
# Restore project
%DOTNETRESTORE%

# Run tests
%DOTNETTEST%

# Copy everything else and build
COPY . ./
%DOTNETPUBLISH%

# Build runtime image
FROM %APPIMAGE%
WORKDIR /app
COPY --from=build-env /app/out .
%ENTRYPOINT%
";

            var currentDir = Directory.GetCurrentDirectory();
            if (opts.SolutionPath == null)
                Console.WriteLine($"-s --sln not provided, using current directory {currentDir}");

            var slnPath = (opts.SolutionPath ?? currentDir).Trim();
            var solutionFiles = Directory.GetFiles(slnPath, "*.sln");
            var solutionFile = ConsoleTools.SelectOne(solutionFiles, s => s, "Select Solution file");
            var path = Path.GetDirectoryName(solutionFile);
            using var sln = new Sln(solutionFile, SlnItems.Projects);

            var nugetConfigs =
                Directory
                    .EnumerateFiles(path, "nuget.config", SearchOption.AllDirectories)
                    .Select(p => Path.GetRelativePath(path, p));

            var copyNugetConfigs = DockerCopyItems(nugetConfigs);

            var projects =
                sln
                    .Result
                    .ProjectItems
                    .ToList();

            var projectFile = ConsoleTools.SelectOne(projects.Where(p => !p.ToPath().Contains("test",StringComparison.InvariantCultureIgnoreCase)), p => p.name, "Select project to run");
            var paths = projects.Select(p => p.ToPath());
            var copyProjects = DockerCopyItems(paths);

            var app = Path.GetFileNameWithoutExtension(projectFile.ToPath());
            app = Path.GetFileName(app);

            var test = ConsoleTools.AskYesNo("Run tests?");

            var buildImages = new[]
            {
                "mcr.microsoft.com/dotnet/core/sdk:3.1",
                "mcr.microsoft.com/dotnet/sdk:5.0",
            };
            
            var appImages = new[]
            {
                "mcr.microsoft.com/dotnet/core/aspnet:3.1",
                "mcr.microsoft.com/dotnet/aspnet:5.0",
                "mcr.microsoft.com/dotnet/core/runtime:3.1",
                "mcr.microsoft.com/dotnet/runtime:5.0"
            };

            var buildImage = ConsoleTools.SelectOne(buildImages, s => s, "Select build image");
            var appImage = ConsoleTools.SelectOne(appImages, s => s, "Select app image");

            var runRestore = $"RUN dotnet restore \"{projectFile.ToPath()}\"";
            var runPublish = $"RUN dotnet publish -c Release -o out \"{projectFile.ToPath()}\"";
            var entrypoint = $"ENTRYPOINT [\"dotnet\", \"{app}.dll\"]";
            var runTests = test ? "RUN dotnet test" : "#RUN dotnet test";

            template = template
                    .Replace("%COPYNUGET%", copyNugetConfigs)
                    .Replace("%COPYCSPROJ%", copyProjects)
                    .Replace("%DOTNETRESTORE%", runRestore)
                    .Replace("%DOTNETPUBLISH%", runPublish)
                    .Replace("%ENTRYPOINT%", entrypoint)
                    .Replace("%DOTNETTEST%", runTests)
                    .Replace("%BUILDIMAGE%",buildImage)
                    .Replace("%APPIMAGE%",appImage)
                ;

            var dockerFilePath = Path.Combine(path, "dockerfile");
            File.WriteAllText(dockerFilePath, template);
            Console.WriteLine($"Wrote docker file to '{dockerFilePath}'");
        }

        private static string DockerCopyItems(IEnumerable<string> paths)
        {
            var sb = new StringBuilder();
            foreach (var p in paths) sb.AppendLine($"COPY {p} ./{p}");

            return sb.ToString();
        }


        private static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }
    }
}