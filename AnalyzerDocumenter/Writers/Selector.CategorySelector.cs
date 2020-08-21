namespace AnalyzerDocumenter.Writers
{
    internal abstract partial class Selector
    {
        private sealed class CategorySelector : Selector
        {
            public override string GetDescription(string? context, RulesetKind rulesetKind)
                => rulesetKind switch
                {
                    RulesetKind.Default => $"Rules in the {context} category with default severity. Rules with IsEnabledByDefault = false or from a different category are disabled.",
                    RulesetKind.Disabled => $"Rules in the {context} category are forced disabled.",
                    RulesetKind.Enabled => $"Rules in the {context} category are enabled with default severity. Rules in the {context} category are force enabled with default severity. Rules from a different category are disabled.",
                    _ => "Unknown"
                };

            public override string GetTitle(string? context, RulesetKind rulesetKind)
                => rulesetKind switch
                {
                    RulesetKind.Default => $"Rules in the {context} category with default severity",
                    RulesetKind.Disabled => $"Rules in the {context} category disabled",
                    RulesetKind.Enabled => $"Rules in the {context} category Enabled with default severity",
                    _ => "Unknown"
                };

            public override bool IsSelected(string? context, RuleDescriptor rule, RulesetKind rulesetKind)
                => rule.Diagnostic.Category == context;
        }
    }
}
