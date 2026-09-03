using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;

namespace Fawkes.Api.Controllers
{

    [Authorize]
    [ApiController]
    public class DeviceManagementController
    {

        /// <summary>
        /// Returns all display devices used for a fixture.
        /// </summary>
        /// <param name="fixtureId">The id of the fixture for which to retrieve devices.</param>
        /// <returns>List of devices</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("fixtures/{fixtureId}/devices/")]
        public async Task<ActionResult<IEnumerable<DeviceBase>>> GetDevicesForFixtureAsync(int fixtureId)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }


        /// <summary>
        /// Assigns a device to a fixture.
        /// </summary>
        /// <param name="request">The request object containing the details of the device to assign.</param>
        /// <returns>The details of the assigned device.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureId}/devices/assign")]
        public async Task<ActionResult<GetDeviceResponse>> AssignDeviceToFixtureAsync(AssignDeviceToFixtureRequest request)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            Match
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
