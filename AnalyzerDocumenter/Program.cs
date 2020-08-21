using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AnalyzerDocumenter.Writers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace AnalyzerDocumenter
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            Func<DirectoryInfo> getCurrentDirectory = () => new DirectoryInfo(Directory.GetCurrentDirectory());

            var rootCommand = new RootCommand("Roslyn analyzers documenter")
            {
                new Option<FileInfo[]>(
                    aliases: new[] { "--assemblies", "-a" },
                    description: "The assemblies with Roslyn analizers")
                {
                    IsRequired = true,
                },
                new Option<string?>(
                    aliases: new[] { "--name", "-n" },
                    description: "The root name of the output assets"),
                new Option<Generators>(
                    aliases: new[] { "--generate", "-g" },
                    description: "The output types to generate"),
                new Option<DirectoryInfo>(
                    aliases: new[] { "--documentation-directory", "-dd" },
                    description: "The analyzer documentation directory",
                    getDefaultValue: getCurrentDirectory),
                new Option<DirectoryInfo>(
                    aliases: new[] { "--sarif-directory", "-sd" },
                    description: "The analyzer SARIF directory",
                    getDefaultValue: getCurrentDirectory),
                new Option<DirectoryInfo>(
                    aliases: new[] { "--rulesets-directory", "-rd" },
                    description: "The analyzer rulesets directory",
                    getDefaultValue: getCurrentDirectory),
                new Option<DirectoryInfo>(
                    aliases: new[] { "--editorconfig-directory", "-ed" },
                    description: "The analyzer .editorconfig base directory",
                    getDefaultValue: getCurrentDirectory),
                new Option<DirectoryInfo>(
                    aliases: new[] { "--build-directory", "-bd" },
                    description: "The MSBuild artifacts directory",
                    getDefaultValue: getCurrentDirectory),
                new Option<DirectoryInfo>(
                    aliases: new[] { "--checks-directory", "-cd" },
                    description: "The checks directory",
                    getDefaultValue: getCurrentDirectory),
                new Option<string[]?>(
                    aliases: new[] { "--tags", "-t" },
                    description: "The analyzer tags to generate rulesets and .editorconfig files"),
            };

            rootCommand.Handler = CommandHandler.Create(typeof(Program)!.GetMethod(nameof(CommandHandlerAsync), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!);

            return await rootCommand.InvokeAsync(args);
        }

        static async Task<int> CommandHandlerAsync(
            FileInfo[] assemblies,
            string? name,
            Generators generate,
            DirectoryInfo documentationDirectory,
            DirectoryInfo sarifDirectory,
            DirectoryInfo rulesetsDirectory,
            DirectoryInfo editorconfigDirectory,
            DirectoryInfo buildDirectory,
            DirectoryInfo checksDirectory,
            string[]? tags)
        {
            if (generate == Generators.None)
            {
                return 0;
            }

            var allRulesById = new SortedList<string, RuleDescriptor>();
            var fixableDiagnosticIds = new HashSet<string>();
            var categories = new HashSet<string>();
            var assemblyDescriptors = new SortedList<string, AssemblyDescriptor>();
            foreach (var assemblyFilePath in EnumerateAssemblyFilePaths(assemblies))
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

            if (generate.HasFlag(Generators.Sarif))
            {
                perAssemblyWriters.Add(new SarifWriter(getFilePath(sarifDirectory, name + ".sarif")));
            }

            if (generate.HasFlag(Generators.Checks))
            {
                perAssemblyWriters.Add(new ChecksWriter(getFilePath(checksDirectory, "Checks.md")));
            }

            if (generate.HasFlag(Generators.Documentation))
            {
                perRuleWriters.Add(new DocumentationWriter(getFilePath(documentationDirectory, name + ".md"), name!, fixableDiagnosticIds));
            }

            if (generate.HasFlag(Generators.MSBuild))
            {
                perRuleWriters.Add(new PropsWriter(getFilePath(buildDirectory, name + ".props")));
            }

            if (generate.HasFlag(Generators.Rulesets))
            {
                perRuleWriters.Add(
                    new RulesetWriter(
                        filePath: getFilePath(rulesetsDirectory, "AllRules" + nameof(RulesetKind.Default) + ".ruleset"),
                        name: name!,
                        rulesetKind: RulesetKind.Default,
                        selector: Selector.All,
                        context: null));

                perRuleWriters.Add(
                    new RulesetWriter(
                        filePath: getFilePath(rulesetsDirectory, "AllRules" + nameof(RulesetKind.Enabled) + ".ruleset"),
                        name: name!,
                        rulesetKind: RulesetKind.Enabled,
                        selector: Selector.All,
                        context: null));
                
                perRuleWriters.Add(
                    new RulesetWriter(
                        filePath: getFilePath(rulesetsDirectory, "AllRules" + nameof(RulesetKind.Disabled) + ".ruleset"),
                        name: name!,
                        rulesetKind: RulesetKind.Disabled,
                        selector: Selector.All,
                        context: null));

                foreach (var category in categories)
                {
                    perRuleWriters.Add(
                        new RulesetWriter(
                            filePath: getFilePath(rulesetsDirectory, category + "Rules" + nameof(RulesetKind.Default) + ".ruleset"),
                            name: name!,
                            rulesetKind: RulesetKind.Default,
                            selector: Selector.Categories,
                            context: category));
                    
                    perRuleWriters.Add(
                        new RulesetWriter(
                            filePath: getFilePath(rulesetsDirectory, category + "Rules" + nameof(RulesetKind.Enabled) + ".ruleset"),
                            name: name!,
                            rulesetKind: RulesetKind.Enabled,
                            selector: Selector.Categories,
                            context: category));
                }

                if (!(tags is null))
                {
                    foreach (var tag in tags)
                    {
                        perRuleWriters.Add(
                            new RulesetWriter(
                                filePath: getFilePath(rulesetsDirectory, tag + "Rules" + nameof(RulesetKind.Default) + ".ruleset"),
                                name: name!,
                                rulesetKind: RulesetKind.Default,
                                selector: Selector.Tags,
                                context: tag));
                        
                        perRuleWriters.Add(
                            new RulesetWriter(
                                filePath: getFilePath(rulesetsDirectory, tag + "Rules" + nameof(RulesetKind.Enabled) + ".ruleset"),
                                name: name!,
                                rulesetKind: RulesetKind.Enabled,
                                selector: Selector.Tags,
                                context: tag));
                    }
                }
            }

            if (generate.HasFlag(Generators.Editorconfig))
            {
                perRuleWriters.Add(
                    new EditorconfigWriter(
                        filePath: getFilePath(getDirectory(editorconfigDirectory, "AllRules" + nameof(RulesetKind.Default)), ".editorconfig"),
                        name: name!,
                        rulesetKind: RulesetKind.Default,
                        selector: Selector.All,
                        context: null));
                
                perRuleWriters.Add(
                    new EditorconfigWriter(
                        filePath: getFilePath(getDirectory(editorconfigDirectory, "AllRules" + nameof(RulesetKind.Enabled)), ".editorconfig"),
                        name: name!,
                        rulesetKind: RulesetKind.Enabled,
                        selector: Selector.All,
                        context: null));
                
                perRuleWriters.Add(
                    new EditorconfigWriter(
                        filePath: getFilePath(getDirectory(editorconfigDirectory, "AllRules" + nameof(RulesetKind.Disabled)), ".editorconfig"),
                        name: name!,
                        rulesetKind: RulesetKind.Disabled,
                        selector: Selector.All,
                        context: null));

                foreach (var category in categories)
                {
                    perRuleWriters.Add(
                        new EditorconfigWriter(
                            filePath: getFilePath(getDirectory(editorconfigDirectory, category + "Rules" + nameof(RulesetKind.Default)), ".editorconfig"),
                            name: name!,
                            rulesetKind: RulesetKind.Default,
                            selector: Selector.Categories,
                            context: category));
                    
                    perRuleWriters.Add(
                        new EditorconfigWriter(
                            filePath: getFilePath(getDirectory(editorconfigDirectory, category + "Rules" + nameof(RulesetKind.Enabled)), ".editorconfig"),
                            name: name!,
                            rulesetKind: RulesetKind.Enabled,
                            selector: Selector.Categories,
                            context: category));
                }

                if (!(tags is null))
                {
                    foreach (var tag in tags)
                    {
                        perRuleWriters.Add(
                            new EditorconfigWriter(
                                filePath: getFilePath(getDirectory(editorconfigDirectory, tag + "Rules" + nameof(RulesetKind.Default)), ".editorconfig"),
                                name: name!,
                                rulesetKind: RulesetKind.Default,
                                selector: Selector.Tags,
                                context: tag));
                        
                        perRuleWriters.Add(
                            new EditorconfigWriter(
                                filePath: getFilePath(getDirectory(editorconfigDirectory, tag + "Rules" + nameof(RulesetKind.Enabled)), ".editorconfig"),
                                name: name!,
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

                var g = fp.IndexOf('*');

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
