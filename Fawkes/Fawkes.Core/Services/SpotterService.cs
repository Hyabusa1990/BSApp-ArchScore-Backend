using Fawkes.Core.Model;
using Fawkes.Core.Rules;
using Fawkes.Core.Abstractions;

namespace Fawkes.Core.Services
{

    public interface ISpotterService
    {
        Task<SpotterTargetData> GetTargetDataAsync(Guid fixtureUniqueId, int targetNo);
        Task<SpotterTargetData> ProcessShotsAsync(Guid fixtureUniqueId, int targetNo, string shots);
        Task<SpotterTargetData> ConfirmEndScoreAsync(Guid fixtureUniqueId, int targetNo);
    }

    public class SpotterService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory) : ISpotterService
    {
        // Once every set is confirmed, CurrentSetNo points past the last set; stay on the last one instead.
        private static int GetCurrentSetNo(TargetData targetData, Scoresheet scoresheet)
            => Math.Min(targetData.CurrentSetNo, scoresheet.Sets.Count());


        public async Task<SpotterTargetData> GetTargetDataAsync(Guid fixtureUniqueId, int targetNo)
        {
            var fixture = await dataStore.GetFixtureByUniqueIdAsync(fixtureUniqueId);
            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with UniqueId {fixtureUniqueId} not found.");
            if (fixture.CurrentRoundNo == 0)
                throw new InvalidOperationException($"Fixture with UniqueId {fixtureUniqueId} has not started any rounds yet.");

            var targetData = await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, targetNo);
            if (targetData == null)
                throw new KeyNotFoundException($"Target assignment for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} not found.");

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
                IsConfirmed = scoresheet[setNo].IsConfirmed
            };
        }

        public async Task<SpotterTargetData> ProcessShotsAsync(Guid fixtureUniqueId, int targetNo, string shots)
        {
            var fixture = await dataStore.GetFixtureByUniqueIdAsync(fixtureUniqueId);
            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with UniqueId {fixtureUniqueId} not found.");
            if (fixture.CurrentRoundNo == 0)
                throw new InvalidOperationException($"Fixture with UniqueId {fixtureUniqueId} has not started any rounds yet.");

            var targetData = await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, targetNo);
            if (targetData == null)
                throw new KeyNotFoundException($"Target assignment for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} not found.");

            var ruleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);

            var scoresheet = ruleSet.EvaluateScoresheet(targetData);
            var setNo = GetCurrentSetNo(targetData, scoresheet);

            if (scoresheet[setNo].IsConfirmed)
                throw new InvalidOperationException($"All sets for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} have already been confirmed.");

            scoresheet[setNo].Shots = shots;
            scoresheet[setNo].Score = ruleSet.EvaluateScore(shots);


            await dataStore.SaveShotsAsync(fixture.Id, fixture.CurrentRoundNo, targetNo, scoresheet.AllShots);


            return new SpotterTargetData
            {
                TargetNo = targetNo,
                CurrentSetNo = setNo,
                TeamName = targetData.TeamName,
                Shots = scoresheet[setNo].Shots,
                CurrentSetScore = scoresheet[setNo].Score,
                IsConfirmed = scoresheet[setNo].IsConfirmed
            };
        }


        public async Task<SpotterTargetData> ConfirmEndScoreAsync(Guid fixtureUniqueId, int targetNo)
        {
            var fixture = await dataStore.GetFixtureByUniqueIdAsync(fixtureUniqueId);
            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with UniqueId {fixtureUniqueId} not found.");
            if (fixture.CurrentRoundNo == 0)
                throw new InvalidOperationException($"Fixture with UniqueId {fixtureUniqueId} has not started any rounds yet.");

            var targetData = await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, targetNo);
            if (targetData == null)
                throw new KeyNotFoundException($"Target assignment for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} not found.");

            var ruleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);

            var scoresheet = ruleSet.EvaluateScoresheet(targetData);
            var setNo = GetCurrentSetNo(targetData, scoresheet);

            if (scoresheet[setNo].Score == null)
                throw new InvalidOperationException($"Cannot confirm score for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} because the score is not yet complete.");

            if (scoresheet[setNo].IsConfirmed)
                throw new InvalidOperationException($"Score for TargetNo {targetNo} in Fixture {fixtureUniqueId} and round number {fixture.CurrentRoundNo} has already been confirmed.");






            await dataStore.SaveConfirmedSetScoreAsync(fixture.Id, fixture.CurrentRoundNo, targetNo, setNo, scoresheet[setNo].Score);


            return new SpotterTargetData
            {
                TargetNo = targetNo,
                CurrentSetNo = setNo,
                TeamName = targetData.TeamName,
                Shots = scoresheet[setNo].Shots,
                CurrentSetScore = scoresheet[setNo].Score,
                IsConfirmed = scoresheet[setNo].IsConfirmed
            };
        }
    }




}
