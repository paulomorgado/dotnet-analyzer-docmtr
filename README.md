# dotnet-analyzer-docmtr

This is a [dotnet tool](https://docs.microsoft.com/dotnet/core/tools/global-tools-how-to-create "Tutorial: Create a .NET Core tool using the .NET Core CLI") to document [Roslyn analyzers](https://docs.microsoft.com/dotnet/standard/analyzers/ "The Roslyn based Analyzers").

The implementation is based on the [custom tool](https://github.com/dotnet/roslyn-analyzers/tree/master/src/Tools/GenerateDocumentationAndConfigFiles) used by the [roslyn-analyzers](https://github.com/dotnet/roslyn-analyzers/) and can be used on any analyzer or group of analyzers.

```
> dotnet analyzer-docmtr --help
AnalyzerDocumenter:
  Roslyn analyzers documenter

Usage:
  AnalyzerDocumenter [options]

Options:
  -a, --assemblies <assemblies> (REQUIRED)                                              The assemblies with Roslyn analizers
  -n, --name <name>                                                                     The root name of the output assets
  -g, --generate <All|Checks|Documentation|Editorconfig|MSBuild|None|Rulesets|Sarif>    The output types to generate
  -dd, --documentation-directory <documentation-directory>                              The analyzer documentation directory [default: C:\Temp\analyzer-docmtr]
  -sd, --sarif-directory <sarif-directory>                                              The analyzer SARIF directory [default: C:\Temp\analyzer-docmtr]
  -rd, --rulesets-directory <rulesets-directory>                                        The analyzer rulesets directory [default: C:\Temp\analyzer-docmtr]
  -ed, --editorconfig-directory <editorconfig-directory>                                The analyzer .editorconfig base directory [default: C:\Temp\analyzer-docmtr]
  -bd, --build-directory <build-directory>                                              The MSBuild artifacts directory [default: C:\Temp\analyzer-docmtr]
  -cd, --checks-directory <checks-directory>                                            The checks directory [default: C:\Temp\analyzer-docmtr]
  -t, --tags <tags>                                                                     The analyzer tags to generate rulesets and .editorconfig files
  --version                                                                             Show version information
  -?, -h, --help                                                                        Show help and usage information
```

## Options

### -a, --assemblies <assemblies> (REQUIRED)

List of assemblies containing analyzers to document.

The set of analizers will documented as a single package. This is useful for meta-packages like [Microsoft.CodeAnalysis.FxCopAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.FxCopAnalyzers), but single package analyzers might have a common assembly and language specific assemblies.

### -n, --name <name>

Name override. If not specified, the name of the name of the first assembly with analyzers found.

### -g, --generate <All|Checks|Documentation|Editorconfig|MSBuild|None|Rulesets|Sarif>

Type of documentation to generate.

#### Sarif

The [SARIF (Static Analysis Results Interchange Format)](https://sarifweb.azurewebsites.net/) documentation of all rules.

See [Microsoft.CodeAnalysis.FxCopAnalyzers.sarif](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.FxCopAnalyzers/Microsoft.CodeAnalysis.FxCopAnalyzers.sarif) for an example.

#### Documentation

The Markdown documentation of all rules.

See [Microsoft.CodeAnalysis.FxCopAnalyzers.md](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.FxCopAnalyzers/Microsoft.CodeAnalysis.FxCopAnalyzers.md) for an example.

#### Rulesets

A set of [rulesets](https://docs.microsoft.com/visualstudio/code-quality/using-rule-sets-to-group-code-analysis-rules) using various combinations.

##### All rules with default severity

All rules with default severity. Rules with `IsEnabledByDefault = false` are disabled.

##### All rules Enabled with default severity

All rules are enabled with default severity. Rules with `IsEnabledByDefault = false` are force enabled with default severity.

##### All rules disabled

All rules are forced disabled.

##### Rules in the *<category>* category with default severity

Rules in the *<category>* category with default severity. Rules with `IsEnabledByDefault = false` or from a different category are disabled.

##### Rules in the *<category>* category Enabled with default severity

Rules in the *<category>* category are enabled with default severity. Rules in the *<category>* category are force enabled with default severity. Rules from a different category are disabled.

##### Rules tagged *<tag>* with default severity

Rules tagged *<tag>* with default severity. Rules with `IsEnabledByDefault = false` or from a different category are disabled.

(see [--tag](#-t---tags-))

##### Rules tagged *<tag>* Enabled with default severity

Rules tagged *<tag>* are enabled with default severity. Rules tagged *<tag>* are force enabled with default severity. Rules not tagged *<tag>* are disabled.

(see [--tag](#-t---tags-))

#### Editorconfig

A set of [EditorConfig](https://docs.microsoft.com/visualstudio/ide/create-portable-custom-editor-options) using various combinations.

#### MSBuild

A [props](https://docs.microsoft.com/visualstudio/msbuild/msbuild-properties) file with [`WarningsNotAsErrors`](https://docs.microsoft.com/visualstudio/msbuild/common-msbuild-project-properties) declaring all diagnostics to not be treated as errors if treat warnings as errors is set.

#### Checks

A set of sanity checks on the diagnostics documentation.

At the moment it's only `HelpLinkUri` checks.

## -dd, --documentation-directory <documentation-directory>

Directory where the **Documention** files will be created.

## -sd, --sarif-directory <sarif-directory>

Directory where the **Sarif** files will be created.

## -rd, --rulesets-directory <rulesets-directory>

Directory where the **Rulesets** files will be created.

## -ed, --editorconfig-directory <editorconfig-directory>

Root directory where the **Editorconfig** files will be created.

## -bd, --build-directory <build-directory>

Directory where the **MSBuild** files will be created.

## -cd, --checks-directory <checks-directory>

Directory where the **Checks** files will be created.

## -t, --tags <tags>

The list of tags to generate [Rulesets](#rulesets)  and [Editorconfig](#editorconfig).
