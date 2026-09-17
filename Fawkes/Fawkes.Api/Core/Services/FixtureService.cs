using Fawkes.Api.Core.Model;
using Fawkes.Api.Store;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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

    public class FixtureService(IFawkesDataStore dataStore) : IFixtureService
    {

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
            var fixture = await dataStore.CreateFixtureAsync(date, location, leagueName, fixtureName);

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
                return await dataStore.UpdateFixtureAsync(id, date, location, leagueName, fixtureName);
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
                return;
            }
            throw new UnauthorizedAccessException();
        }
    }



}
