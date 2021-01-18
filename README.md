# dotnet-analyzer-docmtr

This is a [dotnet tool](https://docs.microsoft.com/dotnet/core/tools/global-tools-how-to-create "Tutorial: Create a .NET Core tool using the .NET Core CLI") to document [Roslyn analyzers](https://docs.microsoft.com/dotnet/standard/analyzers/ "The Roslyn based Analyzers").

The implementation is based on the [custom tool](https://github.com/dotnet/roslyn-analyzers/tree/master/src/Tools/GenerateDocumentationAndConfigFiles) used by the [roslyn-analyzers](https://github.com/dotnet/roslyn-analyzers/) and can be used on any analyzer or group of analyzers.

```text
AnalyzerDocumenter:
  Roslyn analyzers documenter

Usage:
  AnalyzerDocumenter [options] [<assemblies>...]

Arguments:
  <assemblies>    Input assemblies with Roslyn analizers. Accepts globbing patterns.

Options:
  -n, --name <name>                                      The root name of the output assets
  -o, --output, --output-directory <output-directory>    The output directory. Defaults to the current directory. [default: C:\temp]
  -s, --generate-sarif, --sarif                          Generates the analyzer SARIF documentation. The SARIF file will be generated to the 'documentation' subdirectory of the ouput directory.
  -md, --generate-markdown, --markdown                   Generates the analyzer markdown documentation. The SARIF file will be generated to the 'documentation' subdirectory of the ouput directory.
  -rs, --generate-rulesets, --rulesets                   Generates the analyzer rulesets files. The markdown file will be generated to the 'rulesets' subdirectory of the ouput directory.
  -ec, --editorconfig, --generate-editorconfig           Generates the analyzer .editorconfig files. The '.editorconfig' file will be generated to subdirectories of the 'editorconfig' subdirectory
                                                         of the ouput directory.
  -mb, --generate-msbuild, --msbuild                     Generates the analyzer MSBuild files. The files will be generated to the 'build' subdirectory of the ouput directory.
  -ck, --checks, --generate-checks                       Generates the analyzer checks file. The 'Checks.md' file will be generated to the output directory.
  -t, --tags <generate-markdown>                         The analyzer tags to generate rulesets and .editorconfig files.
  --version                                              Show version information
  -?, -h, --help                                         Show help and usage information
```

## Arguments

### <assemblies> (REQUIRED)

List of assemblies containing analyzers to document.

The set of analizers will documented as a single package. This is useful for meta-packages like [Microsoft.CodeAnalysis.FxCopAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.FxCopAnalyzers), but single package analyzers might have a common assembly and language specific assemblies.

## Options

### -n, --name <name>

Name override. If not specified, the name of the name of the first assembly with analyzers found.

### -s, --sarif

Generates the [SARIF (Static Analysis Results Interchange Format)](https://sarifweb.azurewebsites.net/) documentation of all rules to the `documentation` subdirectory of the output directory.

See [Microsoft.CodeAnalysis.FxCopAnalyzers.sarif](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.FxCopAnalyzers/Microsoft.CodeAnalysis.FxCopAnalyzers.sarif) for an example.

### -md, --documentation

Generates the Markdown documentation of all rules to the `documentation` subdirectory of the output directory.

See [Microsoft.CodeAnalysis.FxCopAnalyzers.md](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.FxCopAnalyzers/Microsoft.CodeAnalysis.FxCopAnalyzers.md) for an example.

### -rs, --rulesets

Generates a set of [rulesets](https://docs.microsoft.com/visualstudio/code-quality/using-rule-sets-to-group-code-analysis-rules) using various combinations to the `rulesets` subdirectory of the output directory.

#### All rules with default severity

All rules with default severity. Rules with `IsEnabledByDefault = false` are disabled.

#### All rules Enabled with default severity

All rules are enabled with default severity. Rules with `IsEnabledByDefault = false` are force enabled with default severity.

#### All rules disabled

All rules are forced disabled.

#### Rules in the *<category>* category with default severity

Rules in the *<category>* category with default severity. Rules with `IsEnabledByDefault = false` or from a different category are disabled.

#### Rules in the *<category>* category Enabled with default severity

Rules in the *<category>* category are enabled with default severity. Rules in the *<category>* category are force enabled with default severity. Rules from a different category are disabled.

#### Rules tagged *<tag>* with default severity

Rules tagged *<tag>* with default severity. Rules with `IsEnabledByDefault = false` or from a different category are disabled.

(see [--tags](#-t---tags-))

#### Rules tagged *<tag>* Enabled with default severity

Rules tagged *<tag>* are enabled with default severity. Rules tagged *<tag>* are force enabled with default severity. Rules not tagged *<tag>* are disabled.

(see [--tags](#-t---tags-))

### -ed, --editorconfig

Generates a set of [EditorConfig](https://docs.microsoft.com/visualstudio/ide/create-portable-custom-editor-options) using various combinations to subdirectories of the `editorconfig` subdirectory of the output directory.

#### All rules with default severity

All rules with default severity. Rules with `IsEnabledByDefault = false` are disabled.

#### All rules Enabled with default severity

All rules are enabled with default severity. Rules with `IsEnabledByDefault = false` are force enabled with default severity.

#### All rules disabled

All rules are forced disabled.

#### Rules in the *<category>* category with default severity

Rules in the *<category>* category with default severity. Rules with `IsEnabledByDefault = false` or from a different category are disabled.

#### Rules in the *<category>* category Enabled with default severity

Rules in the *<category>* category are enabled with default severity. Rules in the *<category>* category are force enabled with default severity. Rules from a different category are disabled.

#### Rules tagged *<tag>* with default severity

Rules tagged *<tag>* with default severity. Rules with `IsEnabledByDefault = false` or from a different category are disabled.

(see [--tags](#-t---tags-))

#### Rules tagged *<tag>* Enabled with default severity

Rules tagged *<tag>* are enabled with default severity. Rules tagged *<tag>* are force enabled with default severity. Rules not tagged *<tag>* are disabled.

(see [--tags](#-t---tags-))

### -mb, --msbuild

Generates a [props](https://docs.microsoft.com/visualstudio/msbuild/msbuild-properties) file with [`WarningsNotAsErrors`](https://docs.microsoft.com/visualstudio/msbuild/common-msbuild-project-properties) declaring all diagnostics to not be treated as errors if treat warnings as errors is set to the `build` subdirectory of the output directory..

### -t, --tags <tags>

The list of tags to generate [--rulesets](#-rs---rulesets)  and [-ec---editorconfig](#editorconfig).
