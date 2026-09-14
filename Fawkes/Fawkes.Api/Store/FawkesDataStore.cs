using Fawkes.Api.Authentication;
using Fawkes.Api.Controllers;
using Fawkes.Api.Core;
using Microsoft.EntityFrameworkCore;

namespace Fawkes.Api.Store
{


    public interface IFawkesDataStore
    {
        Task<Device> CreateNewDevice(string deviceCode);
        Task<Device?> GetDeviceByCodeAsync(string deviceCode);
        Task<Fixture?> GetFixtureAsync(int id);
        Task<IEnumerable<Fixture>> GetFixturesForUserAsync(string user);
        Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName);
        Task GrantFixtureAccessAsync(int id, string userName, AccessLevel accessLevel);
        Task<Fixture> UpdateFixtureAsync(int id, DateTime date, string location, string leagueName, string fixtureName);
        Task DeleteFixtureAsync(int id);
        Task<bool> CheckWriteAccessToFixtureAsync(int fixtureId, string user);
        Task<bool> CheckReadAccessToFixtureAsync(int fixtureId, string user);
        Task<bool> CheckOwnerAccessToFixtureAsync(int fixtureId, string user);
        Task<IEnumerable<FixtureUser>> GetUsersForFixtureAsync(int fixtureId);
        Task<IEnumerable<Device>> GetDevicesForFixtureAsync(int fixtureId);
        Task<Device> GetDeviceByIdAsync(int deviceId);
        Task UpdateDeviceAsync(Device device);
    }

    public class FawkesDataStore(FawkesDbContext context) : IFawkesDataStore
    {

        public async Task<Device> CreateNewDevice(string deviceCode)
        {
            var newDevice = new FawkesDbContext.Device()
            {
                Code = deviceCode,
                DisplayType = FawkesDbContext.DisplayType.None,
                Fixture = null
            };

            context.Devices.Add(newDevice);

            await context.SaveChangesAsync();

            return new Device()
            {
                Id = newDevice.Id,
                FixtureId = newDevice.FixtureId,
                DisplayType = ConvertDisplayType(newDevice.DisplayType),
                Code = newDevice.Code
            };


        }


        public async Task<Device?> GetDeviceByCodeAsync(string deviceCode)
        {
            var device = await context.Devices.FirstOrDefaultAsync(d => d.Code == deviceCode);
            if (device == null)
            {
                return null;
            }

            return ConvertDevice(device);
        }

        public async Task<Fixture?> GetFixtureAsync(int id)
        {
            var dbFixture = await context.Fixtures.FindAsync(id);

            if (dbFixture == null)
            {
                return null;
            }


            return ConvertFixture(dbFixture);
        }

        public async Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName)
        {

            var newFixture = new FawkesDbContext.Fixture()
            {
                UniqueId = Guid.NewGuid(),
                Date = date,
                LeagueName = leagueName,
                FixtureName = fixtureName,
                Location = location
            };

            context.Fixtures.Add(newFixture);

            await context.SaveChangesAsync();

            return ConvertFixture(newFixture);
        }





        private static Fixture ConvertFixture(FawkesDbContext.Fixture dbFixture)
        {
            return new Fixture()
            {
                Id = dbFixture.Id,
                UniqueId = dbFixture.UniqueId,
                Date = dbFixture.Date,
                LeagueName = dbFixture.LeagueName,
                FixtureName = dbFixture.FixtureName
            };
        }

        public async Task<IEnumerable<Fixture>> GetFixturesForUserAsync(string user)
        {
            var fixtures = await context.FixturePermissions
                .Where(fp => fp.User == user)
                .Select(fp => fp.Fixture)
                .ToListAsync();

            return fixtures.Select(ConvertFixture).ToArray();
        }



        public async Task<bool> CheckWriteAccessToFixtureAsync(int fixtureId, string user)
        {
            return await context.FixturePermissions.AnyAsync(fp => fp.FixtureId == fixtureId && fp.User == user && fp.AccessLevel >= FawkesDbContext.AccessLevel.Write);
        }
        public async Task<bool> CheckReadAccessToFixtureAsync(int fixtureId, string user)
        {
            return await context.FixturePermissions.AnyAsync(fp => fp.FixtureId == fixtureId && fp.User == user && fp.AccessLevel >= FawkesDbContext.AccessLevel.Read);
        }
        public async Task<bool> CheckOwnerAccessToFixtureAsync(int fixtureId, string user)
        {
            return await context.FixturePermissions.AnyAsync(fp => fp.FixtureId == fixtureId && fp.User == user && fp.AccessLevel >= FawkesDbContext.AccessLevel.Owner);
        }



        private DisplayTheme ConvertDisplayTheme(FawkesDbContext.DisplayTheme displayTheme)
        {

            return displayTheme switch
            {
                FawkesDbContext.DisplayTheme.Dark => DisplayTheme.Dark,
                FawkesDbContext.DisplayTheme.Light => DisplayTheme.Light,
                _ => throw new ArgumentOutOfRangeException(nameof(displayTheme), $"Not expected display theme value: {displayTheme}")
            };
    
        }

        private DisplayType ConvertDisplayType(FawkesDbContext.DisplayType displayType)
        {
            return displayType switch
            {
                FawkesDbContext.DisplayType.None => DisplayType.None,
                FawkesDbContext.DisplayType.Match => DisplayType.Match,
                FawkesDbContext.DisplayType.Table => DisplayType.Table,
                _ => throw new ArgumentOutOfRangeException(nameof(displayType), $"Not expected display type value: {displayType}"),
            };
        }

        public async Task GrantFixtureAccessAsync(int id, string userName, AccessLevel accessLevel)
        {
            var fixture = await context.Fixtures.FindAsync(id);

            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with id {id} not found.");

            var existingPermission = await context.FixturePermissions
                .FirstOrDefaultAsync(fp => fp.FixtureId == id && fp.User == userName);

            if (existingPermission == null && accessLevel != AccessLevel.None)
            {
                var fixturePermission = new FawkesDbContext.FixturePermission()
                {
                    FixtureId = id,
                    User = userName,
                    Fixture = fixture,
                    AccessLevel = ConvertAccessLevel(accessLevel)

                };

                context.FixturePermissions.Add(fixturePermission);
            }
            else if (existingPermission != null && accessLevel == AccessLevel.None)
            {
                context.FixturePermissions.Remove(existingPermission);
            }
            else if (existingPermission != null)
            {
                existingPermission.AccessLevel = ConvertAccessLevel(accessLevel);
            }

            await context.SaveChangesAsync();
        }

        private static FawkesDbContext.AccessLevel ConvertAccessLevel(AccessLevel accessLevel)
        {
            return accessLevel switch
            {
                AccessLevel.None => FawkesDbContext.AccessLevel.None,
                AccessLevel.Read => FawkesDbContext.AccessLevel.Read,
                AccessLevel.Write => FawkesDbContext.AccessLevel.Write,
                AccessLevel.Owner => FawkesDbContext.AccessLevel.Owner,
                _ => throw new ArgumentOutOfRangeException(nameof(accessLevel), $"Not expected access level value: {accessLevel}"),
            };
        }

        public async Task<Fixture> UpdateFixtureAsync(int id, DateTime date, string location, string leagueName, string fixtureName)
        {
            var fixture = await context.Fixtures.FindAsync(id);

            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with id {id} not found.");

            fixture.Date = date;
            fixture.Location = location;
            fixture.LeagueName = leagueName;
            fixture.FixtureName = fixtureName;

            await context.SaveChangesAsync();

            return ConvertFixture(fixture);
        }

        public async Task DeleteFixtureAsync(int id)
        {
            var fixture = await context.Fixtures.FindAsync(id);

            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with id {id} not found.");

            context.Fixtures.Remove(fixture);

            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<FixtureUser>> GetUsersForFixtureAsync(int fixtureId)
        {
            var users = (await context.FixturePermissions
                .Where(fp => fp.FixtureId == fixtureId)
                .ToArrayAsync())
                .Select(fp => new FixtureUser
                {
                    UserName = fp.User,
                    AccessLevel = ConvertAccessLevel(fp.AccessLevel)
                }).ToArray();
            return users;
        }

        private AccessLevel ConvertAccessLevel(FawkesDbContext.AccessLevel accessLevel)
        {
            return accessLevel switch
            {
                FawkesDbContext.AccessLevel.None => AccessLevel.None,
                FawkesDbContext.AccessLevel.Read => AccessLevel.Read,
                FawkesDbContext.AccessLevel.Write => AccessLevel.Write,
                FawkesDbContext.AccessLevel.Owner => AccessLevel.Owner,
                _ => throw new ArgumentOutOfRangeException(nameof(accessLevel), $"Not expected access level value: {accessLevel}"),
            };
        }

        public async Task<IEnumerable<Device>> GetDevicesForFixtureAsync(int fixtureId)
        {
            var devices = (await context.Devices
                .Where(d => d.FixtureId == fixtureId)
                .ToArrayAsync())
                .Select(ConvertDevice).ToArray();
            return devices;
        }

        private Device ConvertDevice(FawkesDbContext.Device d)
        {
            return new Device
            {
                Id = d.Id,
                FixtureId = d.FixtureId,
                Code = d.Code,
                DisplayType = ConvertDisplayType(d.DisplayType),
                DisplayTheme = ConvertDisplayTheme(d.DisplayTheme)
            };
        }

        public async Task<Device> GetDeviceByIdAsync(int deviceId)
        {
            var device = await context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                throw new KeyNotFoundException($"Device with id {deviceId} not found.");
            }
            return ConvertDevice(device);
        }

        public async Task UpdateDeviceAsync(Device device)
        {
            var existingDevice = await context.Devices.FindAsync(device.Id);
            if (existingDevice == null)
            {
                throw new KeyNotFoundException($"Device with id {device.Id} not found.");
            }

            existingDevice.FixtureId = device.FixtureId;
            existingDevice.Code = device.Code;
            existingDevice.DisplayType = ConvertDisplayType(device.DisplayType);
            existingDevice.DisplayTheme = ConvertDisplayTheme(device.DisplayTheme);
            existingDevice.MatchNo = device.MatchNo;

            await context.SaveChangesAsync();
        }

        private FawkesDbContext.DisplayTheme ConvertDisplayTheme(DisplayTheme displayTheme)
        {
            return displayTheme switch
            {
                DisplayTheme.Dark => FawkesDbContext.DisplayTheme.Dark,
                DisplayTheme.Light => FawkesDbContext.DisplayTheme.Light,
                _ => throw new ArgumentOutOfRangeException(nameof(displayTheme), $"Not expected display theme value: {displayTheme}")
            };
        }

        private FawkesDbContext.DisplayType ConvertDisplayType(DisplayType displayType)
        {
            return displayType switch
            {
                DisplayType.None => FawkesDbContext.DisplayType.None,
                DisplayType.Match => FawkesDbContext.DisplayType.Match,
                DisplayType.Table => FawkesDbContext.DisplayType.Table,
                _ => throw new ArgumentOutOfRangeException(nameof(displayType), $"Not expected display type value: {displayType}")
            };
        }
    }
}
