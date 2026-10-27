using Fawkes.Core.Exceptions;
using Fawkes.Core.Model;
using Fawkes.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fawkes.Core.Services
{
    public interface IFixtureService
    {
        Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName, string userName);
        Task DeleteFixtureAsync(int id, string userName);
        Task<Fixture> GetFixtureAsync(int id, string userName);
        Task<IEnumerable<Fixture>> GetFixturesForUserAsync(string userName);
        Task<Fixture> UpdateFixtureAsync(int id, DateTime date, string location, string leagueName, string fixtureName, string userName);
        Task<IEnumerable<FixtureUser>> GetUsersForFixtureAsync(int fixtureId, string userName);
        Task AddUserToFixtureAsync(int id, string requestedUserName, string requestingUserName);
        Task RemoveUserFromFixtureAsync(int id, string requestedUserName, string requestingUserName);
        Task SetPhaseAsync(int fixtureId, int roundNo, string userName);
        Task<int> GetRoundCountAsync(int fixtureId, string userName);
    }

    public class FixtureService(IFawkesDataStore dataStore, ILeagueTableService leagueTableService, IUserDirectory userDirectory, ILogger<FixtureService> logger) : IFixtureService
    {
        // Npgsql akzeptiert für "timestamp with time zone" nur Kind=Utc. Datum ohne Zone
        // (Kind=Unspecified, z. B. "2026-10-10") wird als UTC interpretiert.
        private static DateTime ToUtc(DateTime date) => date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };


        public async Task<Fixture> GetFixtureAsync(int id, string user)
        {
            await dataStore.EnsureReadAccessAsync(id, user);

            return await dataStore.GetFixtureAsync(id)
                ?? throw new NotFoundException($"Fixture {id} not found.", "FIXTURE_NOT_FOUND");
        }
        public async Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName, string userName)
        {
            var fixture = await dataStore.CreateFixtureAsync(ToUtc(date), location, leagueName, fixtureName);

            await dataStore.GrantFixtureAccessAsync(fixture.Id, userName, AccessLevel.Owner);

            logger.LogInformation("User {UserName} created fixture {FixtureId} '{FixtureName}' ({LeagueName}) on {Date:yyyy-MM-dd} in {Location}",
                userName, fixture.Id, fixtureName, leagueName, fixture.Date, location);

            return fixture;
        }

        public async Task<IEnumerable<Fixture>> GetFixturesForUserAsync(string user)
        {
            return await dataStore.GetFixturesForUserAsync(user);
        }

        public async Task<Fixture> UpdateFixtureAsync(int id, DateTime date, string location, string leagueName, string fixtureName, string userName)
        {
            await dataStore.EnsureWriteAccessAsync(id, userName);

            var fixture = await dataStore.UpdateFixtureAsync(id, ToUtc(date), location, leagueName, fixtureName);

            logger.LogInformation("User {UserName} updated fixture {FixtureId}", userName, id);

            return fixture;
        }

        public async Task DeleteFixtureAsync(int id, string userName)
        {
            await dataStore.EnsureOwnerAccessAsync(id, userName);

            await dataStore.DeleteFixtureAsync(id);

            logger.LogInformation("User {UserName} deleted fixture {FixtureId}", userName, id);
        }

        public async Task<IEnumerable<FixtureUser>> GetUsersForFixtureAsync(int fixtureId, string userName)
        {
            await dataStore.EnsureReadAccessAsync(fixtureId, userName);

            return await dataStore.GetUsersForFixtureAsync(fixtureId);
        }

        public async Task AddUserToFixtureAsync(int id, string requestedUserName, string requestingUserName)
        {
            await dataStore.EnsureOwnerAccessAsync(id, requestingUserName);

            if (!await userDirectory.UserExistsAsync(requestedUserName))
                throw new NotFoundException($"User {requestedUserName} not found.", "USER_NOT_FOUND");

            // Owner nicht auf Write herabstufen (sonst hat die Veranstaltung keinen Owner mehr).
            if (await dataStore.CheckOwnerAccessToFixtureAsync(id, requestedUserName))
            {
                logger.LogDebug("User {RequestedUserName} is already owner of fixture {FixtureId}; access level unchanged", requestedUserName, id);
                return;
            }

            await dataStore.GrantFixtureAccessAsync(id, requestedUserName, AccessLevel.Write);

            logger.LogInformation("User {UserName} granted write access on fixture {FixtureId} to {RequestedUserName}", requestingUserName, id, requestedUserName);
        }

        public async Task RemoveUserFromFixtureAsync(int id, string requestedUserName, string requestingUserName)
        {
            await dataStore.EnsureOwnerAccessAsync(id, requestingUserName);

            if (requestedUserName == requestingUserName)
            {
                throw new ConflictException("Owners cannot remove themselves from a fixture.", "OWNER_CANNOT_REMOVE_SELF");
            }

            if (!await dataStore.CheckReadAccessToFixtureAsync(id, requestedUserName))
                throw new NotFoundException($"User {requestedUserName} has no access to fixture {id}.", "USER_NOT_FOUND");

            await dataStore.GrantFixtureAccessAsync(id, requestedUserName, AccessLevel.None);

            logger.LogInformation("User {UserName} removed {RequestedUserName} from fixture {FixtureId}", requestingUserName, requestedUserName, id);
        }

        public async Task<int> GetRoundCountAsync(int fixtureId, string userName)
        {
            await dataStore.EnsureReadAccessAsync(fixtureId, userName);

            return await dataStore.GetRoundCountAsync(fixtureId);
        }

        public async Task SetPhaseAsync(int fixtureId, int roundNo, string userName)
        {
            await dataStore.EnsureWriteAccessAsync(fixtureId, userName);

            // Round 0 means "not started"; otherwise the round must exist in the match play chart.
            var roundCount = await dataStore.GetRoundCountAsync(fixtureId);
            if (roundNo < 0 || roundNo > roundCount)
                throw new InvalidRequestException($"Round number must be between 0 and {roundCount} (was {roundNo}).", "INVALID_ROUND_NO");

            var fixture = await dataStore.GetFixtureAsync(fixtureId)
                ?? throw new NotFoundException($"Fixture {fixtureId} not found.", "FIXTURE_NOT_FOUND");

            if (fixture.CurrentRoundNo == roundNo)
            {
                logger.LogDebug("Fixture {FixtureId} is already in round {RoundNo}; nothing to change", fixtureId, roundNo);
                return;
            }

            await dataStore.UpdateFixtureAsync(fixtureId, roundNo: roundNo);

            logger.LogInformation("User {UserName} moved fixture {FixtureId} from round {PreviousRoundNo} to round {RoundNo}", userName, fixtureId, fixture.CurrentRoundNo, roundNo);

            await leagueTableService.RecalculateAsync(fixture);
        }
    }



}
