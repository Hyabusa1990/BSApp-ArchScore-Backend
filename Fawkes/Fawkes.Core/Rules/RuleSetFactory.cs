using Fawkes.Core.Rules.League;
using Fawkes.Core.Rules.MatchPlay;
using Fawkes.Core.Rules.Scoring;

namespace Fawkes.Core.Rules
{

    public interface IRuleSetFactory
    {
        IScoringRuleSet GetScoringRuleSet(string key);

        IMatchPlayRuleSet GetMatchPlayRuleSet(string key);

        ILeagueRuleSet GetLeagueRuleSet(string key);

    }

    public class RuleSetFactory : IRuleSetFactory
    {
        public ILeagueRuleSet GetLeagueRuleSet(string key)
        {
            return new StandardLeagueRuleSet();
        }

        public IMatchPlayRuleSet GetMatchPlayRuleSet(string key)
        {
            return new SetSystemMatchPlayRuleSetBase(5, false);
        }

        public IScoringRuleSet GetScoringRuleSet(string key)
        {
            return new ScoringRuleSetBase(5, 6, 0, false);
        }
    }

    public static class RuleSetKeys
    {
        public const string Default = "DEFAULT";
    }
}
