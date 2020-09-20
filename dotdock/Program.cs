using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using net.r_eg.MvsSln;

namespace dotdock
{
    class Program
    {
        public class Options
        {
            [Option('s', "sln", Required = false, HelpText = "Solution file to be processed.")]
            public string SolutionPath { get; set; }
            
            
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }

        static void RunOptions(Options opts)
        {
            var template = @"
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy nugetconfigs 
%COPYNUGET%
# Copy csprojs 
%COPYCSPROJ%
# Restore project
%DOTNETRESTORE%

# Copy everything else and build
COPY . ./
%DOTNETPUBLISH%

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/sdk:3.1
WORKDIR /app
COPY --from=build-env /app/out .
%ENTRYPOINT%

".Replace("'", "\"");

            var currentDir = Directory.GetCurrentDirectory();
            if (opts.SolutionPath == null)
            {
                Console.WriteLine($"-s --sln not provided, using current directory {currentDir}");
            }

            var slnPath = (opts.SolutionPath ?? currentDir).Trim();
            var solutionFiles = Directory.GetFiles(slnPath, "*.sln");
            var solutionFile=SelectOne(solutionFiles, s=>s,"Select Solution file");
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
            
            var projectFile = SelectOne(projects, p => p.path,"Select project to run");
            var paths = projects.Select(p => p.path.Replace("\\", "/"));
            var copyProjects = DockerCopyItems(paths);

            var app = Path.GetFileNameWithoutExtension(projectFile.path).Replace("\\", "/");
            app = Path.GetFileName(app);
            
            var runRestore = $"RUN dotnet restore \"{projectFile.path.Replace("\\", "/")}\"";
            var runPublish = $"RUN dotnet publish -c Release -o out \"{projectFile.path.Replace("\\", "/")}\"";
            var entrypoint = "ENTRYPOINT [\"dotnet\", \"" + app + ".dll\"]";
            
            template = template
                .Replace("%COPYNUGET%", copyNugetConfigs)
                .Replace("%COPYCSPROJ%", copyProjects)
                .Replace("%DOTNETRESTORE%", runRestore)
                .Replace("%DOTNETPUBLISH%", runPublish)
                .Replace("%ENTRYPOINT%", entrypoint);
            
            var dockerFilePath = Path.Combine(path, "dockerfile");
            File.WriteAllText(dockerFilePath,template);
            Console.WriteLine($"Wrote docker file to '{dockerFilePath}'");
        }

        private static string DockerCopyItems(IEnumerable<string> paths)
        {
            var sb = new StringBuilder();
            foreach (var p in paths)
            {
                sb.AppendLine($"COPY {p} ./{p}");
            }

            return sb.ToString();
        }

        private static T SelectOne<T>(IEnumerable<T> options, Func<T,string> stringer,string prompt = "Select option")
        {
            Console.WriteLine();
            var arr = options.OrderBy(stringer) .ToArray();
            if (arr.Length == 1)
            {
                var onlyOne = arr.First();
                Console.WriteLine($"Using {stringer(onlyOne)}");
                return onlyOne;
            }
            for (var i = 0; i < arr.Length; i++)
            {
                Console.WriteLine($"{i + 1}) {stringer(arr[i])}");
            }

            Console.WriteLine();
            while (true)
            {
                
                Console.WriteLine(prompt + "> ");
                var res = Console.ReadLine();
                if (int.TryParse(res!.Trim(), out var index) && index >= 1 && index <= arr.Length )
                {
                    return arr[index-1];
                }
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }

    }
}