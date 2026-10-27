using Fawkes.Core.Model;
using Fawkes.Core.Rules;
using Fawkes.Core.Abstractions;

namespace Fawkes.Core.Services
{
    public interface IDosService
    {
        Task<IEnumerable<Match>> GetRoundMatchesAsync(int fixtureId, int roundNo, string userName);
        Task SetConfirmedSetScore(int fixtureId, int roundNo, int targetNo, int setNo, int? score, string userName);
    }

    public class DosService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory) : IDosService
    {
        public async Task<IEnumerable<Match>> GetRoundMatchesAsync(int fixtureId, int roundNo, string userName)
        {
            if (!(await dataStore.CheckReadAccessToFixtureAsync(fixtureId, userName)))
                throw new UnauthorizedAccessException();

            var fixture = await dataStore.GetFixtureAsync(fixtureId);
            if (fixture == null)
                throw new KeyNotFoundException();


            var targets = await dataStore.GetTargetDataAsync(fixtureId, roundNo);

            var scoringRules = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
            var scoresheets = targets.Select(scoringRules.EvaluateScoresheet).ToArray();


            var matchRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);

            var matches = scoresheets.GroupBy(_ => (_.TargetNo + 1) / 2).Select(_ => matchRules.EvaluateMatch(_.ToArray())).ToArray();


            return matches;
        }

        public async Task SetConfirmedSetScore(int fixtureId, int roundNo, int targetNo, int setNo, int? score, string userName)
        {
            if (!(await dataStore.CheckWriteAccessToFixtureAsync(fixtureId, userName)))
                throw new UnauthorizedAccessException();

            await dataStore.SaveSetScoreAsync(fixtureId, roundNo, targetNo, setNo, score);

        }

    }




}
