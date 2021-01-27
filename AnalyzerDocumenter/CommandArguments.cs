using System.CommandLine;
using System.IO;

namespace AnalyzerDocumenter
{
    internal sealed record CommandArguments(
        IConsole Console,
        FileInfo[] Assemblies,
        DirectoryInfo OutputDirectory,
        string? Name,
        string[]? Tags,
        bool GenerateAll,
        bool GenerateSarif,
        bool GenerateMarkdown,
        bool GenerateRulesets,
        bool GenerateEditorConfig,
        bool GenerateGlobalConfig,
        bool GenerateMSBuild,
        bool GenerateChecks
    );
}
