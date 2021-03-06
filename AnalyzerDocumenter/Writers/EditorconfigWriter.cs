﻿using System.Threading.Tasks;

namespace AnalyzerDocumenter.Writers
{
    internal sealed class EditorConfigWriter : ConfigWriterBase
    {
        public EditorConfigWriter(string filePath, RulesetKind rulesetKind, Selector selector, string? context)
            : base(filePath, rulesetKind, selector, context)
        {
        }

        protected sealed override async Task WriteHeaderAsync()
        {
            await this.FileWriter.WriteLineAsync("# NOTE: Requires **VS2019 16.7** or later");
        }

        protected sealed override async Task WriteSectionAsync()
        {
            await this.FileWriter.WriteLineAsync("# Code files");
            await this.FileWriter.WriteLineAsync("[*.{cs,vb}]");
        }
    }
}
