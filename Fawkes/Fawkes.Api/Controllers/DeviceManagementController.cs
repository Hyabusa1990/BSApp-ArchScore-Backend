using Azure.Core;
using Fawkes.Api.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;

namespace Fawkes.Api.Controllers
{

    [Authorize]
    [ApiController]
    public class DeviceManagementController(IDeviceService deviceService) : ControllerBase
    {
        private readonly IDeviceService deviceService = deviceService;

        /// <summary>
        /// Returns all display devices used for a fixture.
        /// </summary>
        /// <param name="fixtureId">The id of the fixture for which to retrieve devices.</param>
        /// <returns>List of devices</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("fixtures/{fixtureId}/devices/")]
        public async Task<ActionResult<IEnumerable<DeviceBase>>> GetDevicesForFixtureAsync(int fixtureId)
        {
            if (User?.Identity?.IsAuthenticated == false || string.IsNullOrEmpty(User?.Identity?.Name))
            {
                return Unauthorized();
            }

            try
            {
                var devices = await deviceService.GetDevicesForFixtureAsync(fixtureId, User.Identity.Name);
                return Ok(devices);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Returns details of a specific device used for a fixture.
        /// </summary>
        /// <param name="fixtureId">The id of the fixture for which to retrieve the device.</param>
        /// <param name="deviceId">The id of the device to retrieve.</param>
        /// <returns>The details of the specified device.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("fixtures/{fixtureId}/devices/{deviceId}")]
        public async Task<ActionResult<GetDeviceResponse>> GetDeviceAsync(int fixtureId, int deviceId)
        {
            if (User?.Identity?.IsAuthenticated == false || string.IsNullOrEmpty(User?.Identity?.Name))
            {
                return Unauthorized();
            }

            try
            {
                var device = await deviceService.GetDeviceForFixtureAsync(fixtureId, deviceId, User.Identity.Name);
                return ConvertDevice(device);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        private GetDeviceResponse ConvertDevice(Device device)
        {
            return new GetDeviceResponse
            {
                Id = device.Id,
                DisplayType = ConvertDisplayType(device.DisplayType),
                DisplayTheme = ConvertDisplayTheme(device.DisplayTheme),
                MatchNo = device.MatchNo

            };
        }

        private DisplayTheme ConvertDisplayTheme(Core.Services.DisplayTheme displayTheme)
        {
            return displayTheme switch
            {
                Core.Services.DisplayTheme.Dark => DisplayTheme.Dark,
                Core.Services.DisplayTheme.Light => DisplayTheme.Light,
                _ => throw new ArgumentOutOfRangeException(nameof(displayTheme), $"Not expected display theme value: {displayTheme}")
            };
        }

        private DisplayType ConvertDisplayType(Core.Model.DisplayType displayType)
        {
            return displayType switch
            {
                Core.Model.DisplayType.None => DisplayType.None,
                Core.Model.DisplayType.Match => DisplayType.Match,
                Core.Model.DisplayType.Table => DisplayType.LeagueTable,
                _ => throw new ArgumentOutOfRangeException(nameof(displayType), $"Not expected display type value: {displayType}")
            };
        }


        /// <summary>
        /// Assigns a device to a fixture.
        /// </summary>
        /// <param name="fixtureId">The id of the fixture for which to assign the device.</param>
        /// <param name="request">The request object containing the details of the device to assign.</param>
        /// <returns>The details of the assigned device.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureId}/devices/assign")]
        public async Task<ActionResult<GetDeviceResponse>> AssignDeviceToFixtureAsync(int fixtureId, AssignDeviceToFixtureRequest request)
        {
            if (User?.Identity?.IsAuthenticated == false || string.IsNullOrEmpty(User?.Identity?.Name))
            {
                return Unauthorized();
            }

            try
            {
                var result = await deviceService.AssignDeviceToFixtureAsync(fixtureId, request.DeviceCode, User.Identity.Name);
                return ConvertDevice(result);
            }
            catch (UnauthorizedAccessException) {
                return Unauthorized();
            }
            catch(KeyNotFoundException)
            { 
                return NotFound();
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }   


        }


        /// <summary>
        /// Removes a device from a fixture.
        /// </summary>
        /// <param name="fixtureId">The id of the fixture from which to remove the device.</param>
        /// <param name="deviceId">The id of the device to remove.</param>
        /// <returns>An ActionResult indicating the result of the unassign operation.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureId}/devices/{deviceId}/unassign")]
        public async Task<ActionResult> UnassignDeviceFromFixtureAsync(int fixtureId, int deviceId)
        {
            if (User?.Identity?.IsAuthenticated == false || string.IsNullOrEmpty(User?.Identity?.Name))
            {
                return Unauthorized();
            }

            try
            {
                await deviceService.RemoveDeviceFromFixtureAsync(fixtureId, deviceId, User.Identity.Name);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// Updates the details of a specific device used for a fixture.    
        /// </summary>
        /// <param name="fixtureId">The id of the fixture for which to update the device.</param>
        /// <param name="deviceId">The id of the device to update.</param>
        /// <param name="request">The request object containing the updated device details.</param>
        /// <returns>The details of the updated device.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureId}/devices/{deviceId}")]
        public async Task<ActionResult<GetDeviceResponse>> UpdateDeviceAsync(int fixtureId, int deviceId, UpdateDeviceRequest request)
        {
            if (User?.Identity?.IsAuthenticated == false || string.IsNullOrEmpty(User?.Identity?.Name))
            {
                return Unauthorized();
            }
            try
            {
                var updatedDevice = await deviceService.UpdateDeviceAsync(fixtureId, deviceId, User.Identity.Name, ConvertDisplayType(request.DisplayType), ConvertDisplayTheme(request.DisplayTheme), request.MatchNo);
                return ConvertDevice(updatedDevice);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private Core.Services.DisplayTheme ConvertDisplayTheme(DisplayTheme displayTheme)
        {
            switch (displayTheme)
            {
                case DisplayTheme.Light:
                    return Core.Services.DisplayTheme.Light;
                case DisplayTheme.Dark:
                    return Core.Services.DisplayTheme.Dark;
                default:
                    throw new ArgumentOutOfRangeException(nameof(displayTheme), displayTheme, null);
            }
        }

        private Core.Model.DisplayType ConvertDisplayType(DisplayType displayType)
        {
            switch (displayType)
            {
                case DisplayType.None:
                    return Core.Model.DisplayType.None;
                case DisplayType.Match:
                    return Core.Model.DisplayType.Match;
                case DisplayType.LeagueTable:
                    return Core.Model.DisplayType.Table;
                default:
                    throw new ArgumentOutOfRangeException(nameof(displayType), displayType, null);
            }   
        }

        public class GetDeviceResponse : DeviceBase
        {
            /// <summary>
            /// The internal id assigned to the device. Used in all crud operations.
            /// </summary>
            public required int Id { get; set; }

        }

        public class UpdateDeviceRequest : DeviceBase
        {
        }




        public abstract class DeviceBase
        {

            /// <summary>
            /// Specifies the type of information to display on the device.
            /// </summary>
            public required DisplayType DisplayType { get; set; }

            public required DisplayTheme DisplayTheme { get; set; } = DisplayTheme.Light;

            /// <summary>
            /// Specifies the match number to display on the device. This property is only relevant when DisplayType is set to Match. If DisplayType is set to Match, this property must be provided and must be a positive integer.
            /// </summary>
            public int? MatchNo { get; set; }
        }



        /// <summary>
        /// Specifies the type of information to display on the device.
        /// </summary>
        public enum DisplayType
        {
            /// <summary>
            /// No information should be displayed on the device.
            /// </summary>
            None,

            /// <summary>
            /// The device should display information related to a match (i.e. two targets). The match number must be specified in the MatchNo property of the device.
            /// </summary>
            Match,

            /// <summary>
            /// The device should display information related to the league table. The MatchNo property of the device is not relevant when DisplayType is set to LeagueTable.
            /// </summary>
            LeagueTable
        }

        /// <summary>
        /// Specifies the theme for the device display.
        /// </summary>
        public enum DisplayTheme
        {
            /// <summary>
            /// Light theme for the device display.
            /// </summary>
            Light,
            /// <summary>
            /// Dark theme for the device display.
            /// </summary>
            Dark
        }

        public class AssignDeviceToFixtureRequest
        {
            /// <summary>
            /// Code assigned to the device.
            /// </summary>
            public required string DeviceCode { get; set; }
        }


    }
}
