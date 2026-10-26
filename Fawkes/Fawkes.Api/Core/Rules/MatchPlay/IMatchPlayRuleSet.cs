using Fawkes.Api.Core.Model;

namespace Fawkes.Api.Core.Rules.MatchPlay
{
    public interface IMatchPlayRuleSet
    {
        int MaxEndNo { get; }

        Match EvaluateMatch(params Scoresheet[] scoresheets);

    }
}
