using Fawkes.Api.Core.Model;
using Fawkes.Api.Core.Rules;
using Fawkes.Api.Store;

namespace Fawkes.Api.Core.Services
{

    public interface ISpotterService
    {
        Task<SpotterTargetData> GetTargetDataAsync(Guid fixtureUniqueId, int targetNo);
        Task<SpotterTargetData> ProcessScoreAsync(Guid fixtureUniqueId, int targetNo, string shots);
    }

    public class SpotterService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory) : ISpotterService
    {
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


            return new SpotterTargetData
            {
                TargetNo = targetNo,
                CurrentSetNo = targetData.CurrentSetNo,
                TeamName = targetData.TeamName,
                Shots = scoresheet[targetData.CurrentSetNo].Shots,
                CurrentSetScore = scoresheet[targetData.CurrentSetNo].Score,
                IsConfirmed = scoresheet[targetData.CurrentSetNo].IsConfirmed
            };
        }

        public async Task<SpotterTargetData> ProcessScoreAsync(Guid fixtureUniqueId, int targetNo, string shots)
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

            scoresheet[targetData.CurrentSetNo].Shots = shots;


            await dataStore.SaveShotsAsync(fixture.Id, fixture.CurrentRoundNo, targetNo, scoresheet.AllShots);


            return new SpotterTargetData
            {
                TargetNo = targetNo,
                CurrentSetNo = targetData.CurrentSetNo,
                TeamName = targetData.TeamName,
                Shots = scoresheet[targetData.CurrentSetNo].Shots,
                CurrentSetScore = scoresheet[targetData.CurrentSetNo].Score,
                IsConfirmed = scoresheet[targetData.CurrentSetNo].IsConfirmed
            };
        }
    }

    public class SpotterTargetData
    {
        public int TargetNo { get; set; }
        public int CurrentSetNo { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Shots { get; set; } = string.Empty;
        public int? CurrentSetScore { get; set; }
        public bool IsConfirmed { get; set; }
    }




}
