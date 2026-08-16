using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DisplayController
    {

        /// <summary>
        /// Registers a device and returns the access token, refresh token, and expiration time. Payload of the access token contains the device code issued during registration.
        /// </summary>
        /// <returns>Access token, refresh token, and expiration time.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("register")]
        public async Task<ActionResult<DeviceTokenResponse>> RegisterDeviceAsync()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Returns the display data for the registered device. The display data includes the display type (Unassigned, None, Match) and any other relevant information.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("data")]
        public async Task<ActionResult<DisplayDataResponse>> GetDisplayDataAsync()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Contains device registration inforamtion including access token, refresh token, and expiration time.
        /// </summary>
        public class DeviceTokenResponse
        {
            /// <summary>
            /// The access token issued to the device upon registration. This token is used for authenticating subsequent requests made by the device to the API.
            /// </summary>
            public required string AccessToken { get; set; }

            /// <summary>
            /// The refresh token issued to the device upon registration. This token can be used to obtain a new access token when the current one expires.
            /// </summary>
            public required string RefreshToken { get; set; }

            /// <summary>
            /// The expiration time of the access token in seconds. This indicates how long the access token is valid before it needs to be refreshed using the refresh token.
            /// </summary>
            public int ExpiresIn { get; set; }
        }

        /// <summary>
        /// Contains display data for the registered device, including the display type and any relevant information about the targets being displayed.
        /// </summary>
        public class DisplayDataResponse
        {
            /// <summary>
            /// The type of display currently being shown on the device.
            /// </summary>
            public required DisplayType DisplayType { get; set; }
            
            /// <summary>
            /// 
            /// </summary>
            public TargetDisplayData[]? Targets { get; set; }
        }

        /// <summary>
        /// Contains data relating to a target being displayed on the device, including the target number, team name, set scores, shots, current set score, and total set points.
        /// </summary>
        public class TargetDisplayData
        {
            /// <summary>
            /// Target number
            /// </summary>
            public int? TargetNo { get; set; }

            /// <summary>
            /// Name of the team.
            /// </summary>
            public string? TeamName { get; set; }

            /// <summary>
            /// List of set scores obtained by the team in the match so far.
            /// </summary>
            public int[]? SetScores { get; set; }

            /// <summary>
            /// Shot values obtained by the team in the current set. 10 is encoded as "+" and "M" is encoded as "0". All other shot values are encoded as their respective integer values. For example, a shot value of 9 is encoded as "9". The shot values are concatenated into a single string. For example, if the team has shot 10, M, and 8 in the current set, the Shots property would be set to "+08".
            /// </summary>
            public string? Shots { get; set; }

            /// <summary>
            /// The current set score of the team in the match. This is the total score obtained by the team in the current set.
            /// </summary>
            public int? CurrentSetScore { get; set; }

            /// <summary>
            /// The total set points obtained by the team in the match. This is the total number of set points obtained by the team in the match so far.
            /// </summary>
            public int? SetPoints { get; set; }
        }


        public enum DisplayType
        {
            /// <summary>
            /// Display device currently not assigned to any fixture.
            /// </summary>
            Unassigned,

            /// <summary>
            /// Display device assigned to a fixture but not currently displaying any information.
            /// </summary>
            None,

            /// <summary>
            /// Display device assigned to a fixture and currently displaying information related to a match (e.g., scores, shots, etc.).
            /// </summary>
            Match
        }



    }
}
