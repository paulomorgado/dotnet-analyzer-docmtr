using System;

namespace AnalyzerDocumenter
{

    [Flags]
    internal enum Generators
    {
        None = 0b0000_0000,
        Documentation = 0b0000_0001,
        Sarif = 0b0000_0010,
        Rulesets = 0b0000_0100,
        Editorconfig = 0b0000_1000,
        MSBuild = 0b0001_0000,
        Checks = 0b0010_0000,
        All = -1,
    }
}
