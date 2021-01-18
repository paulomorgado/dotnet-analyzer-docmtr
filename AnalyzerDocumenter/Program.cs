using AnalyzerDocumenter.Writers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AnalyzerDocumenter
{
    static class Program
    {
        private const string DocumentationOutputDirectory = "documentation";
        private const string MSBuildOutputDirectory = "build";
        private const string RulesetsOutputDirectory = "rulesets";
        private const string EditorconfigOutputDirectory = "editorconfig";
        private const string ChecksFileName = "Checks.md";
        private const string EditorconfigFileName = ".editorconfig";
        private const string SarifFileExtensions = ".sarif";
        private const string MarkdownFileExtension = ".md";
        private const string MSBuildPropsFileExtension = ".props";
        private const string RuleSetFileExtension = ".ruleset";
        private const string AllRulesFilePrefix = "AllRules";
        private const string RulesFilePrefix = "Rules";

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Roslyn analyzers documenter")
            {
                new Argument<FileInfo[]>(
                    name: "assemblies",
                    description: "Input assemblies with Roslyn analizers. Accepts globbing patterns."),
                new Option(
                    aliases: new[] { "--name", "-n" },
                    description: "The root name of the output assets")
                {
                    Argument = new Argument<string?>(name: "name")
                },
                new Option(
                    aliases: new[] { "--output-directory", "--output", "-o" },
                    description: "The output directory. Defaults to the current directory.")
                {
                    Argument = new Argument<DirectoryInfo>(
                        name: "output-directory",
                        getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()))
                },
                new Option(
                    aliases: new[] { "--generate-sarif", "--sarif", "-s" },
                    description: $"Generates the analyzer SARIF documentation. The SARIF file will be generated to the '{DocumentationOutputDirectory}' subdirectory of the ouput directory.")
                {
                    Argument = new Argument<bool>(name: "generate-sarif")
                },
                new Option(
                    aliases: new[] { "--generate-markdown", "--markdown", "-md" },
                    description: $"Generates the analyzer markdown documentation. The SARIF file will be generated to the '{DocumentationOutputDirectory}' subdirectory of the ouput directory.")
                {
                    Argument = new Argument<bool>(name: "generate-markdown")
                },
                new Option<bool>(
                    aliases: new[] { "--generate-rulesets", "--rulesets", "-rs" },
                    description: $"Generates the analyzer rulesets files. The markdown file will be generated to the '{RulesetsOutputDirectory}' subdirectory of the ouput directory.")
                {
                    Argument = new Argument<bool>(name: "generate-rulesets")
                },
                new Option<bool>(
                    aliases: new[] { "--generate-editorconfig", "--editorconfig", "-ec" },
                    description: $"Generates the analyzer .editorconfig files. The '{EditorconfigFileName}' file will be generated to subdirectories of the '{EditorconfigOutputDirectory}' subdirectory of the ouput directory.")
                {
                    Argument = new Argument<bool>(name: "generate-editorconfig")
                },
                new Option(
                    aliases: new[] { "--generate-msbuild", "--msbuild", "-mb" },
                    description: $"Generates the analyzer MSBuild files. The files will be generated to the '{MSBuildOutputDirectory}' subdirectory of the ouput directory.")
                {
                    Argument = new Argument<bool>(name: "generate-msbuild")
                },
                new Option(
                    aliases: new[] { "--generate-checks", "--checks", "-ck" },
                    description: $"Generates the analyzer checks file. The '{ChecksFileName}' file will be generated to the output directory.")
                {
                    Argument = new Argument<bool>(name: "generate-checks")
                },
                new Option(
                    aliases: new[] { "--tags", "-t" },
                    description: "The analyzer tags to generate rulesets and .editorconfig files.")
                {
                    Argument = new Argument<string[]?>(name: "generate-markdown")
                },
            };

            rootCommand.Handler = CommandHandler.Create<CommandArguments>(CommandHandlerAsync);

            return await rootCommand.InvokeAsync(args);
        }

        static async Task<int> CommandHandlerAsync(CommandArguments arguments)
        {
            if (!(
                arguments.GenerateSarif
                || arguments.GenerateMarkdown
                || arguments.GenerateRulesets
                || arguments.GenerateEditorconfig
                || arguments.GenerateMSBuild
                || arguments.GenerateChecks))
            {
                return 0;
            }

            var name = arguments.Name;
            var allRulesById = new SortedList<string, RuleDescriptor>();
            var fixableDiagnosticIds = new HashSet<string>();
            var categories = new HashSet<string>();
            var assemblyDescriptors = new SortedList<string, AssemblyDescriptor>();
            foreach (var assemblyFilePath in EnumerateAssemblyFilePaths(arguments.Assemblies))
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyFilePath);

                if (assemblyDescriptors.ContainsKey(assemblyName))
                {
                    continue;
                }

                var analyzerFileReference = new AnalyzerFileReference(assemblyFilePath, AnalyzerAssemblyLoader.Instance);
                analyzerFileReference.AnalyzerLoadFailed += AnalyzerFileReference_AnalyzerLoadFailed;
                var analyzers = analyzerFileReference.GetAnalyzersForAllLanguages();

                if (analyzers.Length > 0)
                {
                    var assembly = analyzerFileReference.GetAssembly();
                    var dottedQuadFileVersion = assembly.GetName()?.Version?.ToString();
                    var semanticVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    var idx = semanticVersion?.IndexOfAny(new char[] { '-', '+' });
                    var version = (!idx.HasValue || idx.GetValueOrDefault() < 0) ? semanticVersion : semanticVersion![..idx.GetValueOrDefault()];

                    var assemblydescriptor = new AssemblyDescriptor(
                        assemblyName: assemblyName,
                        version: version,
                        dottedQuadFileVersion: dottedQuadFileVersion,
                        semanticVersion: semanticVersion);

                    foreach (var analyzer in analyzers)
                    {
                        var analyzerType = analyzer.GetType();

                        foreach (var diagnostic in analyzer.SupportedDiagnostics)
                        {
                            var languages = analyzerType.GetCustomAttribute<DiagnosticAnalyzerAttribute>(true)?.Languages;
                            var rule = new RuleDescriptor(
                                diagnostic,
                                analyzerType.Name,
                                languages == null || languages.Length == 0 ? ImmutableArray<string>.Empty : ImmutableArray.Create(languages));
                            allRulesById[diagnostic.Id] = rule;
                            categories.Add(diagnostic.Category);
                            assemblydescriptor.Rules[diagnostic.Id] = rule;
                        }
                    }

                    if (string.IsNullOrEmpty(name))
                    {
                        name = assemblyName;
                    }

                    assemblyDescriptors.Add(assemblyName, assemblydescriptor);

                    foreach (var fixer in analyzerFileReference.GetFixers())
                    {
                        foreach (var id in fixer.FixableDiagnosticIds)
                        {
                            fixableDiagnosticIds.Add(id);
                        }
                    }
                }
            }

            if (allRulesById.Count == 0)
            {
                return 0;
            }

            var perAssemblyWriters = new List<WriterBase>();
            var perRuleWriters = new List<WriterBase>();

            if (arguments.GenerateSarif)
            {
                perAssemblyWriters.Add(new SarifWriter(getFilePath(getDirectory(arguments.OutputDirectory, DocumentationOutputDirectory), name + SarifFileExtensions)));
            }

            if (arguments.GenerateChecks)
            {
                perAssemblyWriters.Add(new ChecksWriter(getFilePath(arguments.OutputDirectory, ChecksFileName)));
            }

            if (arguments.GenerateMarkdown)
            {
                perRuleWriters.Add(new DocumentationWriter(getFilePath(getDirectory(arguments.OutputDirectory, DocumentationOutputDirectory), name + MarkdownFileExtension), name!, fixableDiagnosticIds));
            }

            if (arguments.GenerateMSBuild)
            {
                perRuleWriters.Add(new PropsWriter(getFilePath(getDirectory(arguments.OutputDirectory, MSBuildOutputDirectory), name + MSBuildPropsFileExtension)));
            }

            if (arguments.GenerateRulesets)
            {
                var rulesetsOutputDirectory = getDirectory(arguments.OutputDirectory, RulesetsOutputDirectory);

                perRuleWriters.Add(
                    new RulesetWriter(
                        filePath: getFilePath(rulesetsOutputDirectory, AllRulesFilePrefix + nameof(RulesetKind.Default) + RuleSetFileExtension),
                        name: name!,
                        rulesetKind: RulesetKind.Default,
                        selector: Selector.All,
                        context: null));

                perRuleWriters.Add(
                    new RulesetWriter(
                        filePath: getFilePath(rulesetsOutputDirectory, AllRulesFilePrefix + nameof(RulesetKind.Enabled) + RuleSetFileExtension),
                        name: name!,
                        rulesetKind: RulesetKind.Enabled,
                        selector: Selector.All,
                        context: null));

                perRuleWriters.Add(
                    new RulesetWriter(
                        filePath: getFilePath(rulesetsOutputDirectory, AllRulesFilePrefix + nameof(RulesetKind.Disabled) + RuleSetFileExtension),
                        name: name!,
                        rulesetKind: RulesetKind.Disabled,
                        selector: Selector.All,
                        context: null));

                foreach (var category in categories)
                {
                    perRuleWriters.Add(
                        new RulesetWriter(
                            filePath: getFilePath(rulesetsOutputDirectory, category + RulesFilePrefix + nameof(RulesetKind.Default) + RuleSetFileExtension),
                            name: name!,
                            rulesetKind: RulesetKind.Default,
                            selector: Selector.Categories,
                            context: category));

                    perRuleWriters.Add(
                        new RulesetWriter(
                            filePath: getFilePath(rulesetsOutputDirectory, category + RulesFilePrefix + nameof(RulesetKind.Enabled) + RuleSetFileExtension),
                            name: name!,
                            rulesetKind: RulesetKind.Enabled,
                            selector: Selector.Categories,
                            context: category));
                }

                if ((arguments.Tags is not null) && (arguments.Tags.Length > 0))
                {
                    foreach (var tag in arguments.Tags)
                    {
                        perRuleWriters.Add(
                            new RulesetWriter(
                                filePath: getFilePath(rulesetsOutputDirectory, tag + RulesFilePrefix + nameof(RulesetKind.Default) + RuleSetFileExtension),
                                name: name!,
                                rulesetKind: RulesetKind.Default,
                                selector: Selector.Tags,
                                context: tag));

                        perRuleWriters.Add(
                            new RulesetWriter(
                                filePath: getFilePath(rulesetsOutputDirectory, tag + RulesFilePrefix + nameof(RulesetKind.Enabled) + RuleSetFileExtension),
                                name: name!,
                                rulesetKind: RulesetKind.Enabled,
                                selector: Selector.Tags,
                                context: tag));
                    }
                }
            }

            if (arguments.GenerateEditorconfig)
            {
                var editorcondigOutputDirectory = getDirectory(arguments.OutputDirectory, EditorconfigOutputDirectory);

                perRuleWriters.Add(
                    new EditorconfigWriter(
                        filePath: getFilePath(getDirectory(editorcondigOutputDirectory, AllRulesFilePrefix + nameof(RulesetKind.Default)), EditorconfigFileName),
                        rulesetKind: RulesetKind.Default,
                        selector: Selector.All,
                        context: null));

                perRuleWriters.Add(
                    new EditorconfigWriter(
                        filePath: getFilePath(getDirectory(editorcondigOutputDirectory, AllRulesFilePrefix + nameof(RulesetKind.Enabled)), EditorconfigFileName),
                        rulesetKind: RulesetKind.Enabled,
                        selector: Selector.All,
                        context: null));

                perRuleWriters.Add(
                    new EditorconfigWriter(
                        filePath: getFilePath(getDirectory(editorcondigOutputDirectory, AllRulesFilePrefix + nameof(RulesetKind.Disabled)), EditorconfigFileName),
                        rulesetKind: RulesetKind.Disabled,
                        selector: Selector.All,
                        context: null));

                foreach (var category in categories)
                {
                    perRuleWriters.Add(
                        new EditorconfigWriter(
                            filePath: getFilePath(getDirectory(editorcondigOutputDirectory, category + RulesFilePrefix + nameof(RulesetKind.Default)), EditorconfigFileName),
                            rulesetKind: RulesetKind.Default,
                            selector: Selector.Categories,
                            context: category));

                    perRuleWriters.Add(
                        new EditorconfigWriter(
                            filePath: getFilePath(getDirectory(editorcondigOutputDirectory, category + RulesFilePrefix + nameof(RulesetKind.Enabled)), EditorconfigFileName),
                            rulesetKind: RulesetKind.Enabled,
                            selector: Selector.Categories,
                            context: category));
                }

                if ((arguments.Tags is not null) && (arguments.Tags.Length > 0))
                {
                    foreach (var tag in arguments.Tags)
                    {
                        perRuleWriters.Add(
                            new EditorconfigWriter(
                                filePath: getFilePath(getDirectory(editorcondigOutputDirectory, tag + RulesFilePrefix + nameof(RulesetKind.Default)), EditorconfigFileName),
                                rulesetKind: RulesetKind.Default,
                                selector: Selector.Tags,
                                context: tag));

                        perRuleWriters.Add(
                            new EditorconfigWriter(
                                filePath: getFilePath(getDirectory(editorcondigOutputDirectory, tag + RulesFilePrefix + nameof(RulesetKind.Enabled)), EditorconfigFileName),
                                rulesetKind: RulesetKind.Enabled,
                                selector: Selector.Tags,
                                context: tag));
                    }
                }
            }

            await Task.WhenAll(
                perAssemblyWritersAsync(perAssemblyWriters, assemblyDescriptors.Values),
                perRuleWritersAsync(perRuleWriters, allRulesById.Values));

            return 0;

            static async Task perAssemblyWritersAsync(List<WriterBase> writers, IList<AssemblyDescriptor> assemblyDescriptors)
            {
                var tasks = new Task[writers.Count];

                await whenAll(writers, tasks, w => w.WriteStartAsync());

                foreach (var assemblyDescriptor in assemblyDescriptors)
                {
                    if (assemblyDescriptor.Rules.Count == 0)
                    {
                        continue;
                    }

                    await whenAll(writers, tasks, w => w.WriteStartAnalyzerAsync(assemblyDescriptor));

                    await rulesWritersAsync(writers, tasks, assemblyDescriptor.Rules.Values);

                    await whenAll(writers, tasks, w => w.WriteEndAnalyzerAsync());
                }


                await whenAll(writers, tasks, w => w.WriteEndAsync());
            }

            static async Task perRuleWritersAsync(List<WriterBase> writers, IList<RuleDescriptor> ruleDescriptors)
            {
                var tasks = new Task[writers.Count];

                await whenAll(writers, tasks, w => w.WriteStartAsync());

                await rulesWritersAsync(writers, tasks, ruleDescriptors);

                await whenAll(writers, tasks, w => w.WriteEndAsync());
            }

            static async Task rulesWritersAsync(List<WriterBase> writers, Task[] tasks, IList<RuleDescriptor> ruleDescriptors)
            {
                await whenAll(writers, tasks, w => w.WriteStartRulesAsync(true));

                foreach (var rule in ruleDescriptors)
                {
                    await whenAll(writers, tasks, w => w.WriteRuleAsync(rule));
                }

                await whenAll(writers, tasks, w => w.WriteEndRulesAsync());
            }

            static Task whenAll(List<WriterBase> writers, Task[] tasks, Func<WriterBase, Task> func)
            {
                Array.Clear(tasks, 0, tasks.Length);

                for (var i = writers.Count - 1; i >= 0; i--)
                {
                    tasks[i] = func(writers[i]);
                }

                return Task.WhenAll(tasks);
            }

            static void AnalyzerFileReference_AnalyzerLoadFailed(object? sender, AnalyzerLoadFailureEventArgs e)
            {
                if (e?.Exception is Exception exception)
                {
                    throw exception;
                }
            }

            static string getFilePath(DirectoryInfo directory, string fileName)
            {
                if (!directory.Exists)
                {
                    directory.Create();
                }

                return Path.Combine(directory.FullName, fileName);
            }

            static DirectoryInfo getDirectory(DirectoryInfo workingDirectory, string? directory)
            {
                if (string.IsNullOrEmpty(directory))
                {
                    return workingDirectory;
                }

                if (!Path.IsPathRooted(directory))
                {
                    return workingDirectory.CreateSubdirectory(directory);
                }

                var result = new DirectoryInfo(directory);

                result.Create();

                return result;
            }
        }

        private static readonly char[] PathSepartors = new[] { '/', '\\' };

        private static IEnumerable<string> EnumerateAssemblyFilePaths(FileInfo[] filePatterns)
        {
            foreach (var filePattern in filePatterns)
            {
                var fp = filePattern.FullName;

                var g = fp.IndexOf('*', StringComparison.OrdinalIgnoreCase);

                if (g < 0)
                {
                    yield return Path.GetFullPath(Path.Combine(fp));
                }
                else
                {
                    var s = (g < 0) ? fp.LastIndexOfAny(PathSepartors) : fp.LastIndexOfAny(PathSepartors, g);

                    if (s < 0)
                    {
                        continue;
                    }

                    var wd = new DirectoryInfo(fp[..s]);
                    fp = fp[(s + 1)..];

                    var matcher = new Matcher();
                    matcher.AddInclude(fp);

                    foreach (var match in matcher.Execute(new DirectoryInfoWrapper(wd)).Files)
                    {
                        if (match.Path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !match.Path.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            yield return Path.GetFullPath(Path.Combine(wd.FullName, match.Path));
                        }
                    }
                }
            }
        }
    }
}
