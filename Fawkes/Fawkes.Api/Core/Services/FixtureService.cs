using Fawkes.Api.Core.Model;
using Fawkes.Api.Core.Rules;
using Fawkes.Api.Store;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using static Fawkes.Api.Controllers.FixturesController;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fawkes.Api.Core.Services
{
    public interface IFixtureService
    {
        Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName, string userName);
        Task DeleteFixtureAsync(int id, string userName);
        Task<Fixture?> GetFixtureAsync(int id, string userName);
        Task<IEnumerable<Fixture>> GetFixturesForUserAsync(string userName);
        Task<Fixture> UpdateFixtureAsync(int id, DateTime date, string location, string leagueName, string fixtureName, string userName);
        Task<IEnumerable<FixtureUser>> GetUsersForFixtureAsync(int fixtureId, string userName);
        Task AddUserToFixtureAsync(int id, string requestedUserName, string requestingUserName);
        Task RemoveUserFromFixtureAsync(int id, string requestedUserName, string requestingUserName);
        Task SetPhaseAsync(int fixtureId, int roundNo, string userName);
    }

    public class FixtureService(IFawkesDataStore dataStore, IRuleSetFactory ruleSetFactory) : IFixtureService
    {
        // Npgsql akzeptiert für "timestamp with time zone" nur Kind=Utc. Datum ohne Zone
        // (Kind=Unspecified, z. B. "2026-10-10") wird als UTC interpretiert.
        private static DateTime ToUtc(DateTime date) => date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };


        public async Task<Fixture?> GetFixtureAsync(int id, string user)
        {
            if (await dataStore.CheckReadAccessToFixtureAsync(id, user))
            {
                return await dataStore.GetFixtureAsync(id);
            }
            throw new UnauthorizedAccessException();
        }
        public async Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName, string userName)
        {
            var fixture = await dataStore.CreateFixtureAsync(ToUtc(date), location, leagueName, fixtureName);

            await dataStore.GrantFixtureAccessAsync(fixture.Id, userName, AccessLevel.Owner);


            return fixture;
        }

        public async Task<IEnumerable<Fixture>> GetFixturesForUserAsync(string user)
        {
            return await dataStore.GetFixturesForUserAsync(user);
        }

        public async Task<Fixture> UpdateFixtureAsync(int id, DateTime date, string location, string leagueName, string fixtureName, string userName)
        {
            if (await dataStore.CheckWriteAccessToFixtureAsync(id, userName))
            {
                return await dataStore.UpdateFixtureAsync(id, ToUtc(date), location, leagueName, fixtureName);
            }
            throw new UnauthorizedAccessException();
        }

        public async Task DeleteFixtureAsync(int id, string userName)
        {
            if (await dataStore.CheckOwnerAccessToFixtureAsync(id, userName))
            {
                await dataStore.DeleteFixtureAsync(id);
                return;
            }
            throw new UnauthorizedAccessException();
        }

        public async Task<IEnumerable<FixtureUser>> GetUsersForFixtureAsync(int fixtureId, string userName)
        {
            if (await dataStore.CheckReadAccessToFixtureAsync(fixtureId, userName))
            {
                return await dataStore.GetUsersForFixtureAsync(fixtureId);
            }
            throw new UnauthorizedAccessException();

        }

        public async Task AddUserToFixtureAsync(int id, string requestedUserName, string requestingUserName)
        {
            if (await dataStore.CheckOwnerAccessToFixtureAsync(id, requestingUserName))
            {
                // Owner nicht auf Write herabstufen (sonst hat die Veranstaltung keinen Owner mehr).
                if (await dataStore.CheckOwnerAccessToFixtureAsync(id, requestedUserName))
                    return;

                await dataStore.GrantFixtureAccessAsync(id, requestedUserName, AccessLevel.Write);
                return;
            }
            throw new UnauthorizedAccessException();
        }

        public async Task RemoveUserFromFixtureAsync(int id, string requestedUserName, string requestingUserName)
        {
            if (await dataStore.CheckOwnerAccessToFixtureAsync(id, requestingUserName))
            {
                if (requestedUserName == requestingUserName)
                {
                    throw new InvalidOperationException("Owners cannot remove themselves from a fixture.");
                }

                await dataStore.GrantFixtureAccessAsync(id, requestedUserName, AccessLevel.None);
                return;
            }
            throw new UnauthorizedAccessException();
        }

        public async Task SetPhaseAsync(int fixtureId, int roundNo, string userName)
        {
            if (await dataStore.CheckWriteAccessToFixtureAsync(fixtureId, userName))
            {
                await dataStore.UpdateFixtureAsync(fixtureId, roundNo: roundNo);
                await UpdateLeagueTableAsync(fixtureId);
                return;
            }
            throw new UnauthorizedAccessException();
        }

        private async Task UpdateLeagueTableAsync(int fixtureId)
        {
            var fixture = await dataStore.GetFixtureAsync(fixtureId);
            if (fixture == null)
            {
                throw new KeyNotFoundException($"Fixture {fixtureId} does not exist.");
            }

            var scoringRuleSet = ruleSetFactory.GetScoringRuleSet(fixture.RuleSetKey);
            var matchRules = ruleSetFactory.GetMatchPlayRuleSet(fixture.RuleSetKey);
            var leagueRules = ruleSetFactory.GetLeagueRuleSet(fixture.RuleSetKey);




            var initialLeagueTable = await dataStore.GetInitialLeagueTableAsync(fixtureId);
            var scoresheets = (await dataStore.GetTargetDataAsync(fixtureId)).Select(scoringRuleSet.EvaluateScoresheet).ToArray();
            var matches = scoresheets.GroupBy(s => new { s.RoundNo, MatchNo = (s.TargetNo+1)/2 }).Select(m => matchRules.EvaluateMatch(m.ToArray())).ToArray();

            var currentLeagueTable = leagueRules.CalculateLeagueTable(initialLeagueTable, matches);


            await dataStore.UpdateLeagueTableAsync(fixtureId, currentLeagueTable);









        }
    }



}
