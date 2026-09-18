using Fawkes.Api.Authentication;
using Fawkes.Api.Controllers;
using Fawkes.Api.Core.Model;
using Fawkes.Api.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace Fawkes.Api.Store
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
        Task SaveShotsAsync(int id, int currentRoundNo, int targetNo, string shots);
        Task SaveConfirmedSetScoreAsync(int id, int currentRoundNo, int targetNo, int currentSetNo, int? score);
        Task<IEnumerable<TargetData>> GetTargetDataForMatchAsync(int fixtureId, int roundNo, int matchNo);
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

        public async Task<Fixture> GetFixtureByUniqueIdAsync(Guid fixtureUniqueId)
        {
            var dbFixture = await context.Fixtures.FirstOrDefaultAsync(f => f.UniqueId == fixtureUniqueId);
            if (dbFixture == null)
            {
                throw new KeyNotFoundException($"Fixture with UniqueId {fixtureUniqueId} not found.");
            }
            return ConvertFixture(dbFixture);
        }

        public async Task<bool> FixtureExistsAsync(int fixtureId)
        {
            return await context.Fixtures.AnyAsync(f => f.Id == fixtureId);
        }

        public async Task<Fixture> CreateFixtureAsync(DateTime date, string location, string leagueName, string fixtureName)
        {

            var newFixture = new FawkesDbContext.Fixture()
            {
                UniqueId = Guid.NewGuid(),
                Date = date,
                LeagueName = leagueName,
                FixtureName = fixtureName,
                Location = location,
                RuleSetKey = RuleSetKeys.Default
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
                FixtureName = dbFixture.FixtureName,
                Location = dbFixture.Location,
                CurrentRoundNo = dbFixture.CurrentRoundNo
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

        public async Task<Fixture> UpdateFixtureAsync(int id, DateTime? date = null, string? location = null, string? leagueName = null, string? fixtureName = null, int? roundNo = null)
        {
            var fixture = await context.Fixtures.FindAsync(id);

            if (fixture == null)
                throw new KeyNotFoundException($"Fixture with id {id} not found.");

            fixture.Date = date ?? fixture.Date;
            fixture.Location = location ?? fixture.Location;
            fixture.LeagueName = leagueName ?? fixture.LeagueName;
            fixture.FixtureName = fixtureName ?? fixture.FixtureName;
            fixture.CurrentRoundNo = roundNo ?? fixture.CurrentRoundNo;

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

        public async Task<bool> MatchPlayChartExistsAsync(int fixtureId)
        {
            return await context.TargetAssignments.AnyAsync(ta => ta.FixtureId == fixtureId) || await context.Teams.AnyAsync(t => t.FixtureId == fixtureId);
        }

        public async Task DeleteMatchPlayChartAsync(int fixtureId)
        {
            await context.TargetAssignments.Where(ta => ta.FixtureId == fixtureId).ExecuteDeleteAsync();
            await context.Teams.Where(t => t.FixtureId == fixtureId).ExecuteDeleteAsync();
        }

        public async Task<Team> AddTeamToFixtureAsync(int fixtureId, Team team)
        {
            var newTeam = new FawkesDbContext.Team()
            {
                FixtureId = fixtureId,
                Name = team.Name,
                MatchPointsWon = team.MatchPointsWon,
                MatchPointsLost = team.MatchPointsLost,
                SetPointsWon = team.SetPointsWon,
                SetPointsLost = team.SetPointsLost
            };

            context.Teams.Add(newTeam);

            await context.SaveChangesAsync();

            return new Team
            {
                Id = newTeam.Id,
                Name = newTeam.Name,
                MatchPointsWon = newTeam.MatchPointsWon,
                MatchPointsLost = newTeam.MatchPointsLost,
                SetPointsWon = newTeam.SetPointsWon,
                SetPointsLost = newTeam.SetPointsLost
            };
        }

        public async Task AssignTeamToTargetAsync(int fixtureId, int roundNo, int targetNo, int teamId)
        {
            var newTargetAssignment = new FawkesDbContext.TargetAssignment()
            {
                FixtureId = fixtureId,
                RoundNo = roundNo,
                TargetNo = targetNo,
                TeamId = teamId
            };
            context.TargetAssignments.Add(newTargetAssignment);
            await context.SaveChangesAsync();
 

        }

        public async Task<TargetData> GetTargetDataAsync(int fixtureId, int roundNo, int targetNo)
        {
            var targetAssignment = await context.TargetAssignments
                .Include(ta => ta.Team)
                .FirstOrDefaultAsync(ta => ta.FixtureId == fixtureId && ta.RoundNo == roundNo && ta.TargetNo == targetNo);

            if (targetAssignment == null)
                return null;

            return ConvertTargetData(targetAssignment);
        }

        private static TargetData ConvertTargetData(FawkesDbContext.TargetAssignment targetAssignment)
        {
            return new TargetData
            {
                RoundNo = targetAssignment.RoundNo,
                TargetNo = targetAssignment.TargetNo,
                TeamName = targetAssignment.Team.Name,
                CurrentSetNo = targetAssignment.CurrentSetNo,
                Shots = targetAssignment.Shots,
                ConfirmedSet01Score = targetAssignment.ConfirmedSet01Score,
                ConfirmedSet02Score = targetAssignment.ConfirmedSet02Score,
                ConfirmedSet03Score = targetAssignment.ConfirmedSet03Score,
                ConfirmedSet04Score = targetAssignment.ConfirmedSet04Score,
                ConfirmedSet05Score = targetAssignment.ConfirmedSet05Score,
                SetPointsTotal = targetAssignment.SetPointsTotal,
                MatchPointsTotal = targetAssignment.MatchPointsTotal
            };
        }

        public async Task SaveShotsAsync(int id, int currentRoundNo, int targetNo, string shots)
        {
            var targetAssignment = await context.TargetAssignments
                .FirstOrDefaultAsync(ta => ta.Id == id && ta.RoundNo == currentRoundNo && ta.TargetNo == targetNo);

            if (targetAssignment == null)
                throw new InvalidOperationException($"TargetAssignment not found for Id={id}, RoundNo={currentRoundNo}, TargetNo={targetNo}");

            targetAssignment.Shots = shots;

            await context.SaveChangesAsync();
        }

        public async Task SaveConfirmedSetScoreAsync(int id, int currentRoundNo, int targetNo, int currentSetNo, int? score)
        {
            var targetAssignment = await context.TargetAssignments
                .FirstOrDefaultAsync(ta => ta.Id == id && ta.RoundNo == currentRoundNo && ta.TargetNo == targetNo);

            if (targetAssignment == null)
                throw new InvalidOperationException($"TargetAssignment not found for Id={id}, RoundNo={currentRoundNo}, TargetNo={targetNo}");

            targetAssignment.SetConfirmedScore(currentSetNo, score);

            await context.SaveChangesAsync();

        }

        public async Task<IEnumerable<TargetData>> GetTargetDataForMatchAsync(int fixtureId, int roundNo, int matchNo)
        {
            var targetNos = new int[] { matchNo*2-1, matchNo * 2 };

            var targetAssignment = await context.TargetAssignments
                .Include(ta => ta.Team)
                .Where(ta => ta.FixtureId == fixtureId && ta.RoundNo == roundNo && targetNos.Contains(ta.TargetNo))
                .ToListAsync();

            return targetAssignment.Select(ConvertTargetData).ToArray();

        }
    }
}
