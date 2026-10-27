using Fawkes.Core.Abstractions;
using Fawkes.Core.Model;
using Fawkes.Core.Rules;
using Microsoft.Extensions.Logging;

namespace Fawkes.Core.Services
{
    public interface ILeagueTableService
    {
        /// <summary>
        /// Calculates the league table from the initial table and all completed rounds and stores it.
        /// </summary>
        Task RecalculateAsync(Fixture fixture);
    }

    public class LeagueTableService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory, ILogger<LeagueTableService> logger) : ILeagueTableService
    {
        public async Task RecalculateAsync(Fixture fixture)
        {
            var scoringRuleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
            var matchRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);
            var leagueRules = ruleSetFactory.GetLeagueRuleSet(fixture.RuleSetKey);

            var initialLeagueTable = await dataStore.GetInitialLeagueTableAsync(fixture.Id);
            var scoresheets = (await dataStore.GetTargetDataAsync(fixture.Id)).Select(scoringRuleSet.EvaluateScoresheet).ToArray();
            var matches = scoresheets.GroupBy(s => new { s.RoundNo, MatchNo = (s.TargetNo + 1) / 2 }).Select(m => matchRules.EvaluateMatch(m.ToArray())).ToArray();
            var currentLeagueTable = leagueRules.CalculateLeagueTable(initialLeagueTable, matches);

            await dataStore.UpdateLeagueTableAsync(fixture.Id, currentLeagueTable);

            var rounds = matches.GroupBy(m => m.RoundNo).OrderBy(r => r.Key).ToArray();
            logger.LogInformation("Recalculated league table of fixture {FixtureId}: counted rounds [{CountedRounds}], left out incomplete rounds [{IncompleteRounds}]",
                fixture.Id,
                string.Join(", ", rounds.Where(r => r.All(m => m.IsComplete)).Select(r => r.Key)),
                string.Join(", ", rounds.Where(r => !r.All(m => m.IsComplete)).Select(r => r.Key)));
        }
    }
}
