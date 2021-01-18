using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AnalyzerDocumenter.Writers
{
    internal class PropsWriter : XmlWriterBase
    {
        private const string WarningsNotAsErrors = nameof(WarningsNotAsErrors);
        private readonly StringBuilder builder = new();

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
            await this.XmlWriter.WriteAttributeStringAsync(null, "Condition", null, "'$(CodeAnalysisTreatWarningsAsErrors)' == 'false'");

            this.builder.Append("$(" + WarningsNotAsErrors + ")");
        }

        protected internal override Task WriteRuleAsync(RuleDescriptor rule)
        {
            this.builder
                .Append(';')
                .Append(rule.Diagnostic.Id);

            return Task.CompletedTask;
        }

        protected internal sealed override async Task WriteEndAsync()
        {

            await this.XmlWriter.WriteElementStringAsync(null, WarningsNotAsErrors, null, this.builder.ToString());
            await this.XmlWriter.WriteEndElementAsync();
            await this.XmlWriter.WriteEndElementAsync();

            await base.WriteEndAsync();
        }
    }
}
