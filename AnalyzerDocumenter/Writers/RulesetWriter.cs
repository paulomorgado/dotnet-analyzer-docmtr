using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AnalyzerDocumenter.Writers
{
    internal class RulesetWriter : XmlWriterBase
    {
        private readonly RulesetKind rulesetKind;
        private readonly string name;
        private readonly Selector selector;
        private readonly string? context;
        private List<RuleDescriptor>? notSelectedRules;

        public RulesetWriter(string filePath, string name, RulesetKind rulesetKind, Selector selector, string? context)
            : base(filePath, true)
        {
            this.rulesetKind = rulesetKind;
            this.name = name;
            this.selector = selector;
            this.context = context;
        }

        protected internal sealed override async Task WriteStartAsync()
        {
            await base.WriteStartAsync();

            await this.XmlWriter.WriteStartElementAsync(null, "RuleSet", null);
            await this.XmlWriter.WriteAttributeStringAsync(null, "Name", null, this.selector.GetTitle(this.context, this.rulesetKind));
            await this.XmlWriter.WriteAttributeStringAsync(null, "Description", null, this.selector.GetDescription(this.context, this.rulesetKind));
            await this.XmlWriter.WriteAttributeStringAsync(null, "ToolsVersion", null, "15.0");

            await this.WriteStartRulesAsync(true);
        }

        protected internal override async Task WriteStartRulesAsync(bool isSelectedRuleGroup)
        {
            if (isSelectedRuleGroup)
            {
                this.notSelectedRules?.Clear();
            }

            if (!(this.context is null))
            {
                await this.XmlWriter.WriteCommentAsync($"{(isSelectedRuleGroup ? this.context : "Other")} rules");
            }

            await this.XmlWriter.WriteStartElementAsync(null, "Rules", null);
            await this.XmlWriter.WriteAttributeStringAsync(null, "AnalyzerId", null, this.name);
            await this.XmlWriter.WriteAttributeStringAsync(null, "RuleNamespace", null, this.name);

            await base.WriteStartRulesAsync(isSelectedRuleGroup);
        }

        protected internal sealed override async Task WriteRuleAsync(RuleDescriptor rule)
            => await this.WriteRuleAsync(rule, this.rulesetKind);

        private Task WriteRuleAsync(RuleDescriptor rule, RulesetKind rulesetKind)
        {
            if (this.selector.IsSelected(this.context, rule, this.rulesetKind))
            {
                return this.WriteRuleAsyncImpl(rule, rulesetKind);
            }
            else
            {
                this.AddToNotSelectedDiagnostics(rule);

                return Task.CompletedTask;
            }
        }

        private async Task WriteRuleAsyncImpl(RuleDescriptor rule, RulesetKind rulesetKind)
        {
            await this.XmlWriter.WriteCommentAsync(rule.Diagnostic.Title.ToString(CultureInfo.CurrentCulture));
            await this.XmlWriter.WriteStartElementAsync(null, "Rule", null);
            await this.XmlWriter.WriteAttributeStringAsync(null, "Id", null, rule.Diagnostic.Id);
            await this.XmlWriter.WriteAttributeStringAsync(null, "Action", null, GetResolvedSeverity(rulesetKind, rule.Diagnostic));
            await this.XmlWriter.WriteEndElementAsync();

            static string GetResolvedSeverity(RulesetKind rulesetKind, DiagnosticDescriptor diagnosticDescriptor)
            {
                return rulesetKind switch
                {
                    RulesetKind.Default => diagnosticDescriptor.IsEnabledByDefault ? diagnosticDescriptor.DefaultSeverity.ToString() : "None",
                    RulesetKind.Enabled => diagnosticDescriptor.DefaultSeverity.ToString(),
                    _ => "None"
                };
            }
        }

        protected internal override async Task WriteEndRulesAsync()
        {
            await this.XmlWriter.WriteEndElementAsync();

            if (!(this.notSelectedRules is null))
            {
                await this.WriteStartRulesAsync(false);

                foreach (var rule in this.notSelectedRules)
                {
                    await this.WriteRuleAsyncImpl(rule, RulesetKind.None);
                }

                this.notSelectedRules = null;

                await this.WriteEndRulesAsync();
            }

            await base.WriteEndRulesAsync();
        }

        protected internal sealed override async Task WriteEndAsync()
        {
            await this.WriteEndRulesAsync();

            await this.XmlWriter.WriteEndElementAsync();

            await base.WriteEndAsync();
        }

        protected void AddToNotSelectedDiagnostics(RuleDescriptor rule)
        {
            (this.notSelectedRules ??= new List<RuleDescriptor>()).Add(rule);
        }
    }
}
