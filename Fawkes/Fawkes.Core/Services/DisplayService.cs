using Fawkes.Core.Model;
using Fawkes.Core.Rules;
using Fawkes.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fawkes.Core.Services
{
    public interface IDisplayService
    {
        Task<DisplayData?> GetDisplayDataAsync(string deviceCode);
    }

    public class DisplayService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory, ILogger<DisplayService> logger) : IDisplayService
    {
        public async Task<DisplayData?> GetDisplayDataAsync(string deviceCode)
        {
            var device = await dataStore.GetDeviceByCodeAsync(deviceCode);

            if (device == null)
                return null;


            var fixture = device.FixtureId.HasValue ? await dataStore.GetFixtureAsync(device.FixtureId.Value) : null;

            // Devices whose fixture is gone show their code again, so they can be reassigned.
            if (fixture == null)
            {
                // Displays poll every second, so nothing in here is logged above debug level.
                if (device.FixtureId.HasValue)
                    logger.LogDebug("Device {DeviceCode} is assigned to fixture {FixtureId}, which no longer exists; showing it as unassigned", device.Code, device.FixtureId);

                return new UnassignedDisplayData()
                {
                    Theme = device.DisplayTheme,
                    DeviceCode = device.Code
                };
            }


            if (device.DisplayType == DisplayType.None)
            {
                return new NoDisplayData()
                {
                    Theme = device.DisplayTheme
                };
            }
            else if (device.DisplayType == DisplayType.Match)
            {
                var targets = (await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo, (int?)(device.MatchNo ?? 1))).ToArray();

                // No teams on these targets yet (match play chart missing or match number beyond the chart): show nothing instead of failing.
                if (targets.Length == 0)
                {
                    logger.LogDebug("Device {DeviceCode} shows match {MatchNo} of round {RoundNo} in fixture {FixtureId}, which has no teams yet",
                        device.Code, device.MatchNo ?? 1, fixture.CurrentRoundNo, fixture.Id);

                    return new NoDisplayData()
                    {
                        Theme = device.DisplayTheme
                    };
                }

                
                var scoringRules = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
                var matchPlayRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);


                var match = matchPlayRules.EvaluateMatch(targets.Select(scoringRules.EvaluateScoresheet).ToArray());

                var setNo = Math.Min(targets.Min(t => t.CurrentSetNo), matchPlayRules.MaxEndNo);





                return new MatchDisplayData()
                {
                    Theme = device.DisplayTheme,
                    MinNoOfSets = match.MinExpectedNoOfSets,                    
                    Targets = match.Scoresheets.Select(s => new MatchDisplayData.MatchTargetDisplayData()
                    {
                        TargetNo = s.TargetNo,
                        CurrentSetNo = setNo,
                        TeamName = s.TeamName,
                        Shots = s[setNo].Shots,
                        CurrentSetScore = s[setNo].Score,
                        SetScores = Enumerable.Range(1, setNo).Select(i  => s[i].Score).Where(_ => _ != null).ToArray(),
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
