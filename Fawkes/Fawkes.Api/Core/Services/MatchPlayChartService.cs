using Fawkes.Api.Core.Model;
using Fawkes.Api.Store;

namespace Fawkes.Api.Core.Services
{

    public interface IMatchPlayChartService
    {
        Task CreateMatchPlayChartAsync(int fixtureId, Team[] teams, int[][]? targetAssignments, bool hardOverride, string userName);
    }


    public class MatchPlayChartService(IFawkesDataStore fawkesDataStore) : IMatchPlayChartService
    {
        private static readonly int[][] DEFAULT_CHART_8 =
        [
            [5,4,2,7,1,8,3,6],
            [3,5,8,4,7,1,6,2],
            [4,7,1,6,2,5,8,3],
            [8,2,7,3,6,4,1,5],
            [7,6,5,8,3,2,4,1],
            [1,3,4,2,8,6,5,7],
            [2,1,6,5,4,3,7,8]
        ];





        public async Task CreateMatchPlayChartAsync(int fixtureId, Team[] teams, int[][]? targetAssignments, bool hardOverride, string userName)
        {
            if (!await fawkesDataStore.CheckWriteAccessToFixtureAsync(fixtureId, userName))
            {
                throw new UnauthorizedAccessException($"User {userName} does not have write access to fixture {fixtureId}.");
            }

            if (!await fawkesDataStore.FixtureExistsAsync(fixtureId))
            {
                throw new InvalidOperationException($"Fixture {fixtureId} does not exist.");
            }

            if (await fawkesDataStore.MatchPlayChartExistsAsync(fixtureId))
            {
                if (!hardOverride)
                {
                    throw new InvalidOperationException($"Match play chart already exists for fixture {fixtureId}. Use hardOverride to force creation.");
                }
                await fawkesDataStore.DeleteMatchPlayChartAsync(fixtureId);
            }

            targetAssignments = targetAssignments ?? GetDefaultMatchPlayChart(teams.Length);

            if (targetAssignments == null)
            {
                throw new InvalidOperationException($"No default match play chart available for {teams.Length} teams.");
            }

            foreach(var team in teams)
            {
                team.Id = (await fawkesDataStore.AddTeamToFixtureAsync(fixtureId, team)).Id;
            }

            for (var roundNo = 1; roundNo <= targetAssignments.Length; roundNo++)
                for (var targetNo = 1; targetNo <= targetAssignments[roundNo - 1].Length; targetNo++)
                {
                    var teamIndex = targetAssignments[roundNo - 1][targetNo - 1] - 1;

                    if (teamIndex < 0 || teamIndex >= teams.Length)
                        continue;

                    await fawkesDataStore.AssignTeamToTargetAsync(fixtureId, roundNo, targetNo, teams[teamIndex].Id);
                }
        }

        private int[][]? GetDefaultMatchPlayChart(int noOfTeams)
        {
            if (noOfTeams == 8 || noOfTeams == 7)
            {
                return DEFAULT_CHART_8;
            }
            return null;

        }

    }

}
