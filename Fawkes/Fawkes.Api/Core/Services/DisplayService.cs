using Fawkes.Api.Core.Model;
using Fawkes.Api.Core.Rules;
using Fawkes.Api.Store;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Fawkes.Api.Core.Services
{
    public interface IDisplayService
    {
        Task<DisplayData?> GetDisplayDataAsync(string deviceCode);
    }

    public class DisplayService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory) : IDisplayService
    {
        public async Task<DisplayData?> GetDisplayDataAsync(string deviceCode)
        {
            var device = await dataStore.GetDeviceByCodeAsync(deviceCode);

            if (device == null)
                return null;


            if (!device.FixtureId.HasValue)
            {
                return new UnassignedDisplayData()
                {
                    Theme = device.DisplayTheme,
                    DeviceCode = device.Code
                };
            }

            var fixture = await dataStore.GetFixtureAsync(device.FixtureId.Value);

            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with ID {device.FixtureId.Value} not found.");


            if (device.DisplayType == DisplayType.None)
            {
                return new NoDisplayData()
                {

                };
            }
            else if (device.DisplayType == DisplayType.Match)
            {
                var targets = await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, (int?)(device.MatchNo ?? 1));

                
                var scoringRules = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
                var matchPlayRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);


                var match = matchPlayRules.EvaluateMatch(targets.Select(scoringRules.EvaluateScoresheet).ToArray());

                var setNo = targets.Min(t => t.CurrentSetNo);



                return new MatchDisplayData()
                {
                    Theme = device.DisplayTheme,
                    Targets = match.Scoresheets.Select(s => new MatchDisplayData.MatchTargetDisplayData()
                    {
                        TargetNo = s.TargetNo,
                        CurrentSetNo = setNo,
                        TeamName = s.TeamName,
                        Shots = s[setNo].Shots,
                        CurrentSetScore = s[setNo].Score,
                        SetScores = Enumerable.Range(1, setNo).Select(i  => s[i].Score).ToArray(),
                        SetPoints = s.SetPoints,
                        IsConfirmed = s[setNo].IsConfirmed
                    }).ToArray()

                };
            }
            else if (device.DisplayType == DisplayType.Table)
            {
                var table = await dataStore.GetLeagueTableAsync(fixture.Id);

                return new TableDisplayData()
                {
                    Theme = device.DisplayTheme,
                    Positions = table.Positions
                };
            }

            throw new NotImplementedException($"Display type {device.DisplayType} is not implemented.");


        }
    }

}
