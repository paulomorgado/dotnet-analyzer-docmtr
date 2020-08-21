using System.Text;
using System.Threading.Tasks;

namespace AnalyzerDocumenter.Writers
{
    internal abstract partial class Selector
    {
        public static readonly Selector All = new AllSelector();

        public static readonly Selector Categories = new CategorySelector();

        public static readonly Selector Tags = new TagSelector();

        public abstract bool IsSelected(string? context, RuleDescriptor rule, RulesetKind rulesetKind);

        public abstract string GetTitle(string? context, RulesetKind rulesetKind);

        public abstract string GetDescription(string? context, RulesetKind rulesetKind);
    }
}
