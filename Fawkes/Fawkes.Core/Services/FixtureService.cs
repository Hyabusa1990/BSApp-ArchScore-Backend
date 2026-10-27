using Fawkes.Core.Exceptions;
using Fawkes.Core.Model;
using Fawkes.Core.Rules;
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
    }

    public class FixtureService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory, ILogger<FixtureService> logger) : IFixtureService
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

            await dataStore.GrantFixtureAccessAsync(id, requestedUserName, AccessLevel.None);

            logger.LogInformation("User {UserName} removed {RequestedUserName} from fixture {FixtureId}", requestingUserName, requestedUserName, id);
        }

        public async Task SetPhaseAsync(int fixtureId, int roundNo, string userName)
        {
            if (roundNo < 0)
                throw new InvalidRequestException($"Round number must not be negative (was {roundNo}).", "INVALID_ROUND_NO");

            await dataStore.EnsureWriteAccessAsync(fixtureId, userName);

            await dataStore.UpdateFixtureAsync(fixtureId, roundNo: roundNo);

            logger.LogInformation("User {UserName} moved fixture {FixtureId} to round {RoundNo}", userName, fixtureId, roundNo);

            await UpdateLeagueTableAsync(fixtureId);
        }

        private async Task UpdateLeagueTableAsync(int fixtureId)
        {
            var fixture = await dataStore.GetFixtureAsync(fixtureId);
            if (fixture == null)
            {
                throw new NotFoundException($"Fixture {fixtureId} not found.", "FIXTURE_NOT_FOUND");
            }

            var scoringRuleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
            var matchRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);
            var leagueRules = ruleSetFactory.GetLeagueRuleSet(fixture.RuleSetKey);




            var initialLeagueTable = await dataStore.GetInitialLeagueTableAsync(fixtureId);
            var scoresheets = (await dataStore.GetTargetDataAsync(fixtureId)).Select(scoringRuleSet.EvaluateScoresheet).ToArray();
            var matches = scoresheets.GroupBy(s => new { s.RoundNo, MatchNo = (s.TargetNo+1)/2 }).Select(m => matchRules.EvaluateMatch(m.ToArray())).ToArray();
            var currentLeagueTable = leagueRules.CalculateLeagueTable(initialLeagueTable, matches);


            await dataStore.UpdateLeagueTableAsync(fixtureId, currentLeagueTable);

            var rounds = matches.GroupBy(m => m.RoundNo).OrderBy(r => r.Key).ToArray();
            logger.LogInformation("Recalculated league table of fixture {FixtureId}: counted rounds [{CountedRounds}], left out incomplete rounds [{IncompleteRounds}]",
                fixtureId,
                string.Join(", ", rounds.Where(r => r.All(m => m.IsComplete)).Select(r => r.Key)),
                string.Join(", ", rounds.Where(r => !r.All(m => m.IsComplete)).Select(r => r.Key)));









        }
    }



}
