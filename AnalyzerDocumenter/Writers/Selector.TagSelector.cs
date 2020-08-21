using System.Linq;

namespace AnalyzerDocumenter.Writers
{
    internal abstract partial class Selector
    {
        private sealed class TagSelector : Selector
        {
            public override string GetDescription(string? context, RulesetKind rulesetKind)
                => rulesetKind switch
                {
                    RulesetKind.Default => $"Rules tagged {context} with default severity. Rules with IsEnabledByDefault = false are disabled.",
                    RulesetKind.Disabled => $"Rules tagged {context} are forced disabled.",
                    RulesetKind.Enabled => $"Rules tagged {context} are enabled with default severity. Rules tagged {context} are force enabled with default severity. Rules not tagged {context} are disabled.",
                    _ => "Unknown"
                };

            public override string GetTitle(string? context, RulesetKind rulesetKind)
                => rulesetKind switch
                {
                    RulesetKind.Default => $"Rules tagged {context} with default severity",
                    RulesetKind.Disabled => $"Rules tagged {context} disabled",
                    RulesetKind.Enabled => $"Rules tagged {context} Enabled with default severity",
                    _ => "Unknown"
                };

            public override bool IsSelected(string? context, RuleDescriptor rule, RulesetKind rulesetKind)
                => rule.Diagnostic.CustomTags.Contains(context);
        }
    }
}
