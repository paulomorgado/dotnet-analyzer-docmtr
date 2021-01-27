using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnalyzerDocumenter.Writers
{
    internal sealed class DocumentationWriter : WriterBase
    {
        private static readonly Regex lineBreaksRegex = new("\r?\n", RegexOptions.Compiled);
        private readonly HashSet<string> fixableDiagnosticIds;
        private readonly string name;

        public DocumentationWriter(string filePath, string name, HashSet<string> fixableDiagnosticIds)
            : base(filePath)
        {
            this.fixableDiagnosticIds = fixableDiagnosticIds;
            this.name = name;
        }

        protected internal override async Task WriteStartRulesAsync(bool isSelectedRuleGroup)
        {
            await this.FileWriter.WriteAsync("# ");
            await this.FileWriter.WriteLineAsync(this.name);
            await this.FileWriter.WriteLineAsync();

            await base.WriteStartRulesAsync(isSelectedRuleGroup);
        }

        protected internal override async Task WriteRuleAsync(RuleDescriptor rule)
        {
            var hasHelpUri = !string.IsNullOrEmpty(rule.Diagnostic.HelpLinkUri);

            await this.FileWriter.WriteAsync("## ");

            if (hasHelpUri)
            {
                await this.FileWriter.WriteAsync("[");
            }

            await this.FileWriter.WriteAsync(rule.Diagnostic.Id);

            if (hasHelpUri)
            {
                await this.FileWriter.WriteAsync("](");
                await this.FileWriter.WriteAsync(rule.Diagnostic.HelpLinkUri);
                await this.FileWriter.WriteAsync(")");
            }

            await this.FileWriter.WriteAsync(": ");
            await this.FileWriter.WriteAsync(rule.Diagnostic.Title.ToString(CultureInfo.InvariantCulture));
            await this.FileWriter.WriteLineAsync();
            await this.FileWriter.WriteLineAsync();

            var description = rule.Diagnostic.Description.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(description))
            {
                description = rule.Diagnostic.MessageFormat.ToString(CultureInfo.InvariantCulture);
            }

            // Replace line breaks with HTML breaks so that new
            // lines don't break the markdown table formatting.
            description = lineBreaksRegex.Replace(description, "<br>");

            await this.FileWriter.WriteLineAsync(description);
            await this.FileWriter.WriteLineAsync();

            await this.FileWriter.WriteLineAsync("|Item|Value|");
            await this.FileWriter.WriteLineAsync("|-|-|");
            await this.FileWriter.WriteAsync("|Category|");
            await this.FileWriter.WriteAsync(rule.Diagnostic.Category);
            await this.FileWriter.WriteLineAsync("|");
            await this.FileWriter.WriteAsync("|Enabled|");
            await this.FileWriter.WriteAsync(rule.Diagnostic.IsEnabledByDefault.ToString(CultureInfo.InvariantCulture));
            await this.FileWriter.WriteLineAsync("|");
            await this.FileWriter.WriteAsync("|Severity|");
            await this.FileWriter.WriteAsync(rule.Diagnostic.DefaultSeverity.ToString());
            await this.FileWriter.WriteLineAsync("|");
            await this.FileWriter.WriteAsync("|CodeFix|");
            await this.FileWriter.WriteAsync(this.fixableDiagnosticIds.Contains(rule.Diagnostic.Id).ToString(CultureInfo.InvariantCulture));
            await this.FileWriter.WriteLineAsync("|");
            await this.FileWriter.WriteLineAsync();
        }
    }
}
