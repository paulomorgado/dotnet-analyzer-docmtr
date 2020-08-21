namespace AnalyzerDocumenter.Writers
{
    internal abstract partial class Selector
    {
        private sealed class AllSelector : Selector
        {
            public override string GetDescription(string? context, RulesetKind rulesetKind)
                => rulesetKind switch
                {
                    RulesetKind.Default => "All rules with default severity. Rules with IsEnabledByDefault = false are disabled.",
                    RulesetKind.Disabled => "All rules are forced disabled.",
                    RulesetKind.Enabled => "All rules are enabled with default severity. Rules with IsEnabledByDefault = false are force enabled with default severity.",
                    _ => "Unknown"
                };

            public override string GetTitle(string? context, RulesetKind rulesetKind)
                => rulesetKind switch
                {
                    RulesetKind.Default => "All rules with default severity",
                    RulesetKind.Disabled => "All rules disabled",
                    RulesetKind.Enabled => "All rules Enabled with default severity",
                    _ => "Unknown"
                };

            public override bool IsSelected(string? context, RuleDescriptor rule, RulesetKind rulesetKind)
                => true;
        }
    }
}
