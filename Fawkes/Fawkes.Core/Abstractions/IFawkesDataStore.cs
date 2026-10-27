using Fawkes.Core.Model;

namespace Fawkes.Core.Abstractions
{
    public interface IFawkesDataStore
    {
        Task<Device> CreateNewDevice(string deviceCode);
        Task<Device?> GetDeviceByCodeAsync(string deviceCode);

        Task<bool> FixtureExistsAsync(int fixtureId);

        Task<Fixture?> GetFixtureAsync(int id);
        Task<Fixture> GetFixtureByUniqueIdAsync(Guid fixtureUniqueId);
        Task<IEnumerable<Fixture>> GetFixturesForUserAsync(string user);
        Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName);
        Task GrantFixtureAccessAsync(int id, string userName, AccessLevel accessLevel);
        Task<Fixture> UpdateFixtureAsync(int id, DateTime? date = null, string? location = null, string? leagueName = null, string? fixtureName = null, int? roundNo = null);
        Task DeleteFixtureAsync(int id);
        Task<bool> CheckWriteAccessToFixtureAsync(int fixtureId, string user);
        Task<bool> CheckReadAccessToFixtureAsync(int fixtureId, string user);
        Task<bool> CheckOwnerAccessToFixtureAsync(int fixtureId, string user);
        Task<IEnumerable<FixtureUser>> GetUsersForFixtureAsync(int fixtureId);
        Task<IEnumerable<Device>> GetDevicesForFixtureAsync(int fixtureId);
        Task<Device> GetDeviceByIdAsync(int deviceId);
        Task UpdateDeviceAsync(Device device);
        Task<bool> MatchPlayChartExistsAsync(int fixtureId);
        Task DeleteMatchPlayChartAsync(int fixtureId);
        Task<Team> AddTeamToFixtureAsync(int fixtureId, Team team);
        Task AssignTeamToTargetAsync(int fixtureId, int roundNo, int targetNo, int teamId);
        Task<TargetData> GetTargetDataAsync(int fixtureId, int roundNo, int targetNo);
        Task SaveShotsAsync(int fixtureId, int currentRoundNo, int targetNo, string shots);
        Task SaveConfirmedSetScoreAsync(int fixtureId, int currentRoundNo, int targetNo, int currentSetNo, int? score);
        Task<IEnumerable<TargetData>> GetTargetDataAsync(int fixtureId, int? roundNo = null, int? matchNo = null);
        Task<LeagueTable> GetInitialLeagueTableAsync(int fixtureId);
        Task<LeagueTable> GetLeagueTableAsync(int fixtureId);
        Task UpdateLeagueTableAsync(int fixtureId, LeagueTable leagueTable);
        Task SaveSetScoreAsync(int fixtureId, int roundNo, int targetNo, int setNo, int? score);

        /// <summary>Highest round number with target assignments; 0 while no match play chart exists.</summary>
        Task<int> GetRoundCountAsync(int fixtureId);

        /// <summary>Number of matches per round (pairs of targets); 0 while no match play chart exists.</summary>
        Task<int> GetMatchCountAsync(int fixtureId);
    }
}
