using System.Threading.Tasks;

namespace AnalyzerDocumenter.Writers
{
    internal sealed class GlobalConfigWriter : ConfigWriterBase
    {
        public GlobalConfigWriter(string filePath, RulesetKind rulesetKind, Selector selector, string? context)
            : base(filePath, rulesetKind, selector, context)
        {
        }

        protected sealed override async Task WriteHeaderAsync()
        {
            await this.FileWriter.WriteLineAsync("# NOTE: Requires **VS2019 16.7** or later");
        }

        protected sealed override async Task WriteSectionAsync()
        {
            await this.FileWriter.WriteLineAsync("is_global = true");
            await this.FileWriter.WriteLineAsync("global_level = -1");
        }
    }
}
