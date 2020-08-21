using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AnalyzerDocumenter
{
    internal sealed class RuleDescriptor
    {
        public RuleDescriptor(DiagnosticDescriptor diagnostic, string typeName, ImmutableArray<string> languages)
        {
            this.Diagnostic = diagnostic;
            this.TypeName = typeName;
            this.Languages = languages;
        }

        public DiagnosticDescriptor Diagnostic { get; }
        public string TypeName { get; }
        public ImmutableArray<string> Languages { get; }
    }
}
