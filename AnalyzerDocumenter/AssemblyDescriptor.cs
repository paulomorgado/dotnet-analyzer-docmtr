using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AnalyzerDocumenter
{
    internal sealed class AssemblyDescriptor
    {
        public AssemblyDescriptor(string assemblyName, string? version, string? dottedQuadFileVersion, string? semanticVersion)
        {
            this.AssemblyName = assemblyName;
            this.Version = version;
            this.DottedQuadFileVersion = dottedQuadFileVersion;
            this.SemanticVersion = semanticVersion;
        }

        public SortedList<string, RuleDescriptor> Rules { get; } = new SortedList<string, RuleDescriptor>();
        public string AssemblyName { get; }
        public string? Version { get; }
        public string? DottedQuadFileVersion { get; }
        public string? SemanticVersion { get; }
    }
}
