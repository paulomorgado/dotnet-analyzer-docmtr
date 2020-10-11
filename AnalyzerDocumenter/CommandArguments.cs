using System.CommandLine;
using System.ComponentModel;
using System.IO;

namespace AnalyzerDocumenter
{
    internal sealed class CommandArguments
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IConsole Console { get; init; }
        public FileInfo[] Assemblies { get; init; }
        public DirectoryInfo OutputDirectory { get; init; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string? Name { get; init; }
        public string[]? Tags { get; init; }
        public bool GenerateSarif { get; init; }
        public bool GenerateMarkdown { get; init; }
        public bool GenerateRulesets { get; init; }
        public bool GenerateEditorconfig { get; init; }
        public bool GenerateMSBuild { get; init; }
        public bool GenerateChecks { get; init; }
    }
}
