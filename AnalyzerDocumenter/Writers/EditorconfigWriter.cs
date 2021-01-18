using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AnalyzerDocumenter.Writers
{
    internal class EditorconfigWriter : WriterBase
    {
        private readonly RulesetKind rulesetKind;
        private readonly Selector selector;
        private readonly string? context;
        private List<RuleDescriptor>? notSelectedRules;

        public EditorconfigWriter(string filePath, RulesetKind rulesetKind, Selector selector, string? context)
            : base(filePath)
        {
            this.rulesetKind = rulesetKind;
            this.selector = selector;
            this.context = context;
        }

        protected internal sealed override async Task WriteStartAsync()
        {
            await base.WriteStartAsync();

            await this.FileWriter.WriteLineAsync("# NOTE: Requires **VS2019 16.3** or later");
            await this.FileWriter.WriteLineAsync();
            await this.FileWriter.WriteAsync("# ");
            await this.FileWriter.WriteLineAsync(this.selector.GetTitle(this.context, this.rulesetKind));
            await this.FileWriter.WriteAsync("# Description: ");
            await this.FileWriter.WriteLineAsync(this.selector.GetDescription(this.context, this.rulesetKind));
            await this.FileWriter.WriteLineAsync();
            await this.FileWriter.WriteLineAsync("# Code files");
            await this.FileWriter.WriteLineAsync("[*.{cs,vb}]");
            await this.FileWriter.WriteLineAsync();
        }

        protected internal override async Task WriteStartRulesAsync(bool isSelectedRuleGroup)
        {
            if (isSelectedRuleGroup)
            {
                this.notSelectedRules?.Clear();
            }

            if (!(this.context is null))
            {
                await this.FileWriter.WriteAsync("# ");
                await this.FileWriter.WriteAsync(isSelectedRuleGroup ? this.context : "Other");
                await this.FileWriter.WriteLineAsync(" rules");
            }
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
            await this.FileWriter.WriteLineAsync();
            await this.FileWriter.WriteAsync("# ");
            await this.FileWriter.WriteAsync(rule.Diagnostic.Id);
            await this.FileWriter.WriteAsync(": ");
            await this.FileWriter.WriteLineAsync(rule.Diagnostic.Title.ToString());
            await this.FileWriter.WriteAsync("dotnet_diagnostic.");
            await this.FileWriter.WriteAsync(rule.Diagnostic.Id);
            await this.FileWriter.WriteAsync(".severity = ");
            await this.FileWriter.WriteLineAsync(GetResolvedSeverity(rulesetKind, rule.Diagnostic));

            static string GetResolvedSeverity(RulesetKind rulesetKind, DiagnosticDescriptor diagnosticDescriptor)
            {
                return rulesetKind switch
                {
                    RulesetKind.Default => diagnosticDescriptor.IsEnabledByDefault ? diagnosticDescriptor.DefaultSeverity.ToString(CultureInfo.InvariantCulture).ToLowerInvariant() : "none",
                    RulesetKind.Enabled => diagnosticDescriptor.DefaultSeverity.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
                    _ => "none"
                };
            }
        }

        protected internal override async Task WriteEndRulesAsync()
        {
            if (!(this.notSelectedRules is null))
            {
                await this.FileWriter.WriteLineAsync();
                await this.FileWriter.WriteLineAsync();
                await this.FileWriter.WriteLineAsync();
                await this.WriteStartRulesAsync(false);

                foreach (var rule in this.notSelectedRules)
                {
                    await this.WriteRuleAsyncImpl(rule, RulesetKind.None);
                }

                this.notSelectedRules = null;

                await this.WriteEndRulesAsync();
            }
        }

        protected internal sealed override async Task WriteEndAsync()
        {
            await this.WriteEndRulesAsync();

            await base.WriteEndAsync();
        }

        protected void AddToNotSelectedDiagnostics(RuleDescriptor rule)
        {
            (this.notSelectedRules ??= new List<RuleDescriptor>()).Add(rule);
        }
    }
}
