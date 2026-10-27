using Fawkes.Core.Exceptions;
using Fawkes.Core.Model;
using Fawkes.Core.Rules;
using Fawkes.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fawkes.Core.Services
{

    public interface ISpotterService
    {
        Task<SpotterTargetData> GetTargetDataAsync(Guid fixtureUniqueId, int targetNo);
        Task<SpotterTargetData> ProcessShotsAsync(Guid fixtureUniqueId, int targetNo, string shots);
        Task<SpotterTargetData> ConfirmEndScoreAsync(Guid fixtureUniqueId, int targetNo);
    }

    public class SpotterService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory, ILogger<SpotterService> logger) : ISpotterService
    {
        // Once every set is confirmed, CurrentSetNo points past the last set; stay on the last one instead.
        private static int GetCurrentSetNo(TargetData targetData, Scoresheet scoresheet)
            => Math.Min(targetData.CurrentSetNo, scoresheet.Sets.Count());

        // A match is decided by both targets of the pair (1+2, 3+4, ...), so the opponent's scores are needed too.
        // confirmedSetNo marks a set of this target as confirmed that is not yet reflected in the loaded data.
        private async Task<bool> IsMatchCompleteAsync(Fixture fixture, int targetNo, int? confirmedSetNo = null)
        {
            var scoringRules = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
            var matchPlayRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);

            var matchNo = (targetNo + 1) / 2;
            var scoresheets = (await dataStore.GetTargetDataAsync(fixture.Id, (int?)fixture.CurrentRoundNo, (int?)matchNo))
                .Select(scoringRules.EvaluateScoresheet)
                .ToArray();

            if (scoresheets.Length == 0)
                return false;

            if (confirmedSetNo.HasValue)
            {
                var ownScoresheet = scoresheets.FirstOrDefault(s => s.TargetNo == targetNo);
                if (ownScoresheet != null)
                    ownScoresheet[confirmedSetNo.Value].IsConfirmed = true;
            }

            var match = matchPlayRules.EvaluateMatch(scoresheets);
            return match.IsComplete;
        }


        public async Task<SpotterTargetData> GetTargetDataAsync(Guid fixtureUniqueId, int targetNo)
        {
            var fixture = await dataStore.GetFixtureByUniqueIdAsync(fixtureUniqueId);
            if (fixture == null)
                throw new NotFoundException($"Fixture with UniqueId {fixtureUniqueId} not found.", "FIXTURE_NOT_FOUND");
            if (fixture.CurrentRoundNo == 0)
                throw new ConflictException($"Fixture with UniqueId {fixtureUniqueId} has not started any rounds yet.", "ROUND_NOT_STARTED");

            var targetData = await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, targetNo);
            if (targetData == null)
                throw new NotFoundException($"Target assignment for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} not found.", "TARGET_NOT_FOUND");

            var ruleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);

            var scoresheet = ruleSet.EvaluateScoresheet(targetData);
            var setNo = GetCurrentSetNo(targetData, scoresheet);


            return new SpotterTargetData
            {
                TargetNo = targetNo,
                CurrentSetNo = setNo,
                TeamName = targetData.TeamName,
                Shots = scoresheet[setNo].Shots,
                CurrentSetScore = scoresheet[setNo].Score,
                IsConfirmed = scoresheet[setNo].IsConfirmed,
                MatchComplete = await IsMatchCompleteAsync(fixture, targetNo)
            };
        }

        public async Task<SpotterTargetData> ProcessShotsAsync(Guid fixtureUniqueId, int targetNo, string shots)
        {
            var fixture = await dataStore.GetFixtureByUniqueIdAsync(fixtureUniqueId);
            if (fixture == null)
                throw new NotFoundException($"Fixture with UniqueId {fixtureUniqueId} not found.", "FIXTURE_NOT_FOUND");
            if (fixture.CurrentRoundNo == 0)
                throw new ConflictException($"Fixture with UniqueId {fixtureUniqueId} has not started any rounds yet.", "ROUND_NOT_STARTED");

            var targetData = await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, targetNo);
            if (targetData == null)
                throw new NotFoundException($"Target assignment for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} not found.", "TARGET_NOT_FOUND");

            var ruleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);

            var scoresheet = ruleSet.EvaluateScoresheet(targetData);
            var setNo = GetCurrentSetNo(targetData, scoresheet);

            if (scoresheet[setNo].IsConfirmed)
                throw new ConflictException($"All sets for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} have already been confirmed.", "ALL_SETS_CONFIRMED");

            scoresheet[setNo].Shots = shots;
            scoresheet[setNo].Score = ruleSet.EvaluateScore(shots);


            await dataStore.SaveShotsAsync(fixture.Id, fixture.CurrentRoundNo, targetNo, scoresheet.AllShots);

            // Called for every arrow, so only at debug level.
            logger.LogDebug("Spotter entered shots '{Shots}' for set {SetNo} on target {TargetNo}, round {RoundNo} of fixture {FixtureId}",
                shots, setNo, targetNo, fixture.CurrentRoundNo, fixture.Id);


            return new SpotterTargetData
            {
                TargetNo = targetNo,
                CurrentSetNo = setNo,
                TeamName = targetData.TeamName,
                Shots = scoresheet[setNo].Shots,
                CurrentSetScore = scoresheet[setNo].Score,
                IsConfirmed = scoresheet[setNo].IsConfirmed,
                // Unconfirmed shots never decide a match, so the stored state is sufficient here.
                MatchComplete = await IsMatchCompleteAsync(fixture, targetNo)
            };
        }


        public async Task<SpotterTargetData> ConfirmEndScoreAsync(Guid fixtureUniqueId, int targetNo)
        {
            var fixture = await dataStore.GetFixtureByUniqueIdAsync(fixtureUniqueId);
            if (fixture == null)
                throw new NotFoundException($"Fixture with UniqueId {fixtureUniqueId} not found.", "FIXTURE_NOT_FOUND");
            if (fixture.CurrentRoundNo == 0)
                throw new ConflictException($"Fixture with UniqueId {fixtureUniqueId} has not started any rounds yet.", "ROUND_NOT_STARTED");

            var targetData = await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, targetNo);
            if (targetData == null)
                throw new NotFoundException($"Target assignment for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} not found.", "TARGET_NOT_FOUND");

            var ruleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);

            var scoresheet = ruleSet.EvaluateScoresheet(targetData);
            var setNo = GetCurrentSetNo(targetData, scoresheet);

            if (scoresheet[setNo].Score == null)
                throw new ConflictException($"Cannot confirm score for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} because the score is not yet complete.", "SET_INCOMPLETE");

            if (scoresheet[setNo].IsConfirmed)
                throw new ConflictException($"Score for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} has already been confirmed.", "SET_ALREADY_CONFIRMED");






            await dataStore.SaveConfirmedSetScoreAsync(fixture.Id, fixture.CurrentRoundNo, targetNo, setNo, scoresheet[setNo].Score);
            scoresheet[setNo].IsConfirmed = true;

            logger.LogInformation("Spotter confirmed set {SetNo} of {TeamName} on target {TargetNo}, round {RoundNo} of fixture {FixtureId} with score {Score}",
                setNo, targetData.TeamName, targetNo, fixture.CurrentRoundNo, fixture.Id, scoresheet[setNo].Score);

            var matchComplete = await IsMatchCompleteAsync(fixture, targetNo, confirmedSetNo: setNo);
            if (matchComplete)
            {
                logger.LogInformation("Match {MatchNo} in round {RoundNo} of fixture {FixtureId} is complete",
                    (targetNo + 1) / 2, fixture.CurrentRoundNo, fixture.Id);
            }

            return new SpotterTargetData
            {
                TargetNo = targetNo,
                CurrentSetNo = setNo,
                TeamName = targetData.TeamName,
                Shots = scoresheet[setNo].Shots,
                CurrentSetScore = scoresheet[setNo].Score,
                IsConfirmed = scoresheet[setNo].IsConfirmed,
                MatchComplete = matchComplete
            };
        }
    }




}
