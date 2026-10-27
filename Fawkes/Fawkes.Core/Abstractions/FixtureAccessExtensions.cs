using Fawkes.Core.Exceptions;

namespace Fawkes.Core.Abstractions
{
    /// <summary>
    /// Access checks that tell a missing fixture (404) apart from a missing permission (403).
    /// The data store's Check*Access methods return false in both cases.
    /// </summary>
    public static class FixtureAccessExtensions
    {
        extension(IFawkesDataStore dataStore)
        {
            public Task EnsureReadAccessAsync(int fixtureId, string userName)
                => EnsureAccessAsync(dataStore, fixtureId, dataStore.CheckReadAccessToFixtureAsync(fixtureId, userName), $"User '{userName}' does not have read access to fixture {fixtureId}.");

            public Task EnsureWriteAccessAsync(int fixtureId, string userName)
                => EnsureAccessAsync(dataStore, fixtureId, dataStore.CheckWriteAccessToFixtureAsync(fixtureId, userName), $"User '{userName}' does not have write access to fixture {fixtureId}.");

            public Task EnsureOwnerAccessAsync(int fixtureId, string userName)
                => EnsureAccessAsync(dataStore, fixtureId, dataStore.CheckOwnerAccessToFixtureAsync(fixtureId, userName), $"User '{userName}' is not an owner of fixture {fixtureId}.");
        }

        private static async Task EnsureAccessAsync(IFawkesDataStore dataStore, int fixtureId, Task<bool> accessCheck, string accessDeniedMessage)
        {
            if (await accessCheck)
                return;

            if (!await dataStore.FixtureExistsAsync(fixtureId))
                throw new NotFoundException($"Fixture {fixtureId} not found.", "FIXTURE_NOT_FOUND");

            throw new AccessDeniedException(accessDeniedMessage);
        }
    }
}
