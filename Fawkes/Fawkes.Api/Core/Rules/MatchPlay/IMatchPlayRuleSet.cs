using Fawkes.Api.Core.Model;

namespace Fawkes.Api.Core.Rules.MatchPlay
{
    public interface IMatchPlayRuleSet
    {
        Match EvaluateMatch(params Scoresheet[] scoresheets);

    }
}
