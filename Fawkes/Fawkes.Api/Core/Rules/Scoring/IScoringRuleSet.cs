using Fawkes.Api.Core.Model;

namespace Fawkes.Api.Core.Rules.Scoring
{
    public interface IScoringRuleSet
    {
        int? EvaluateScore(string shots);

        Scoresheet EvaluateScoresheet(TargetData target);
    }
}
