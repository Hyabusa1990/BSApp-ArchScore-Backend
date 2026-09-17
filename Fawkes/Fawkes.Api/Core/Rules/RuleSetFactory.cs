using Fawkes.Api.Core.Rules.League;
using Fawkes.Api.Core.Rules.MatchPlay;
using Fawkes.Api.Core.Rules.Scoring;
using Microsoft.IdentityModel.Tokens;

namespace Fawkes.Api.Core.Rules
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
            throw new NotImplementedException();
        }

        public IMatchPlayRuleSet GetMatchPlayRuleSet(string key)
        {
            throw new NotImplementedException();
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
