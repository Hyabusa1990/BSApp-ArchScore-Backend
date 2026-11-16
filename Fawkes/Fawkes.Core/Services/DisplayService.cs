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

                var (minNoOfSets, matchTargets) = EvaluateMatch(fixture, targets);

                return new MatchDisplayData()
                {
                    Theme = device.DisplayTheme,
                    MinNoOfSets = minNoOfSets,
                    Targets = matchTargets
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
            else if (device.DisplayType == DisplayType.Overview)
            {
                var table = await dataStore.GetLeagueTableAsync(fixture.Id);

                // Before the first round (round 0) there are no target assignments, so only the table is shown.
                var matches = (await dataStore.GetTargetDataAsync(fixture.Id, fixture.CurrentRoundNo))
                    .GroupBy(t => (t.TargetNo + 1) / 2)
                    .OrderBy(m => m.Key)
                    .Select(m =>
                    {
                        var (minNoOfSets, matchTargets) = EvaluateMatch(fixture, m.ToArray());
                        return new OverviewDisplayData.OverviewMatch()
                        {
                            MatchNo = m.Key,
                            MinNoOfSets = minNoOfSets,
                            Targets = matchTargets
                        };
                    })
                    .ToArray();

                return new OverviewDisplayData()
                {
                    Theme = device.DisplayTheme,
                    Positions = table.Positions,
                    Matches = matches
                };
            }

            throw new NotImplementedException($"Display type {device.DisplayType} is not implemented.");
        }

        // Both targets of a match are shown on the set that is currently being shot: the lower set
        // number of the two, capped at the last set once the match is over.
        private (int MinNoOfSets, MatchDisplayData.MatchTargetDisplayData[] Targets) EvaluateMatch(Fixture fixture, TargetData[] targets)
        {
            var scoringRules = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
            var matchPlayRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);

            var match = matchPlayRules.EvaluateMatch(targets.Select(scoringRules.EvaluateScoresheet).ToArray());

            var setNo = Math.Min(targets.Min(t => t.CurrentSetNo), matchPlayRules.MaxEndNo);

            return (match.MinExpectedNoOfSets, match.Scoresheets.Select(s => new MatchDisplayData.MatchTargetDisplayData()
            {
                TargetNo = s.TargetNo,
                CurrentSetNo = setNo,
                TeamName = s.TeamName,
                Shots = s[setNo].Shots,
                CurrentSetScore = s[setNo].Score,
                SetScores = Enumerable.Range(1, setNo).Select(i => s[i].Score).Where(_ => _ != null).ToArray(),
                SetPoints = s.SetPoints,
                IsConfirmed = s[setNo].IsConfirmed
            }).ToArray());
        }
    }

}
