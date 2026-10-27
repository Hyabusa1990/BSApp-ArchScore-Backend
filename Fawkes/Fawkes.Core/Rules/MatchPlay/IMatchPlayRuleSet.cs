using Fawkes.Core.Model;

namespace Fawkes.Core.Rules.MatchPlay
{
    public interface IMatchPlayRuleSet
    {
        int MaxEndNo { get; }

        Match EvaluateMatch(params Scoresheet[] scoresheets);

    }
}
