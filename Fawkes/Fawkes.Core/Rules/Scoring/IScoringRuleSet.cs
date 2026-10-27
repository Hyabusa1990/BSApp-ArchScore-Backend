using Fawkes.Core.Model;

namespace Fawkes.Core.Rules.Scoring
{
    public interface IScoringRuleSet
    {
        int? EvaluateScore(string shots);

        /// <summary>Highest score a single set can reach (all arrows in the highest ring).</summary>
        int MaxSetScore { get; }

        Scoresheet EvaluateScoresheet(TargetData target);
    }
}
