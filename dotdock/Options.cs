using CommandLine;

namespace dotdock
{
    internal partial class Program
    {
        public class Options
        {
            [Option('s', "sln", Required = false, HelpText = "Solution file to be processed.")]
            public string SolutionPath { get; set; }
        }
    }
}