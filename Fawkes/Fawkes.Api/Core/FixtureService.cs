using Fawkes.Api.Store;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Fawkes.Api.Core
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
    }


    public class Fixture
    {
        public int Id { get; set; }
        public Guid UniqueId { get; set; }
        public DateTime Date { get; set; }
        public string? LeagueName { get; set; }
        public string? FixtureName { get; set; }
        public string? Location { get; set; }
    }

    public class FixtureUser
    {
        public string UserName { get; set; } = string.Empty;
        public AccessLevel AccessLevel { get; set; }
    }

    public enum AccessLevel
    {
        None,
        Read,
        Write,
        Owner
    }



}
