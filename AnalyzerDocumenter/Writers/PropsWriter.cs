using System.Text;
using System.Threading.Tasks;

namespace AnalyzerDocumenter.Writers
{
    internal class PropsWriter : XmlWriterBase
    {
        private const string CodeAnalysisRuleIds = nameof(CodeAnalysisRuleIds);
        private const string WarningsNotAsErrors = nameof(WarningsNotAsErrors);
        private bool isFirst;

        public PropsWriter(string filePath)
            : base(filePath, false)
        {
        }

        protected internal sealed override async Task WriteStartAsync()
        {
            await base.WriteStartAsync();

            await this.XmlWriter.WriteStartElementAsync(null, "Project", null);

            await this.XmlWriter.WriteCommentAsync(@"
    This property group prevents the rule ids implemented in this package to be bumped to errors when
    the 'CodeAnalysisTreatWarningsAsErrors' = 'false'.
");

            await this.XmlWriter.WriteStartElementAsync(null, "PropertyGroup", null);
            await this.XmlWriter.WriteStartElementAsync(null, "CodeAnalysisRuleIds", null);

            this.isFirst = true;
        }

        protected internal override async Task WriteRuleAsync(RuleDescriptor rule)
        {
            if (this.isFirst)
            {
                this.isFirst = false;
            }
            else
            {
                await this.XmlWriter.WriteStringAsync(";");
            }

            await this.XmlWriter.WriteStringAsync(rule.Diagnostic.Id);
        }

        protected internal sealed override async Task WriteEndAsync()
        {

            //await this.XmlWriter.WriteElementStringAsync(null, CodeAnalysisRuleIds, null, this.builder.ToString(0, this.builder.Length - 1));
            await this.XmlWriter.WriteEndElementAsync();

            await this.XmlWriter.WriteStartElementAsync(null, "WarningsNotAsErrors", null);
            await this.XmlWriter.WriteAttributeStringAsync(null, "Condition", null, "'$(CodeAnalysisTreatWarningsAsErrors)' == 'false'");
            await this.XmlWriter.WriteStringAsync("$(WarningsNotAsErrors);$(CodeAnalysisRuleIds)");
            await this.XmlWriter.WriteEndElementAsync();

            await this.XmlWriter.WriteEndElementAsync();

            await base.WriteEndAsync();
        }
    }
}
