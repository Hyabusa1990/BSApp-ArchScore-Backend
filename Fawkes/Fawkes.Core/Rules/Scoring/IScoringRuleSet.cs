using Fawkes.Core.Model;

namespace Fawkes.Core.Rules.Scoring
{
    public interface IScoringRuleSet
    {
        int? EvaluateScore(string shots);

        Scoresheet EvaluateScoresheet(TargetData target);
    }
}
