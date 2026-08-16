using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{
    [ApiController]
    public class SpotterController
    {

        /// <summary>
        /// Retrieves the details of a specific target in a fixture. This action is used to get the current state of a target, including its shots and associated team information.
        /// </summary>
        /// <param name="fixtureUniqueId">The unique identifier of the fixture.</param>
        /// <param name="targetNo">The number of the target.</param>
        /// <returns>The target response containing the current state of the target.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("fixtures/{fixtureUniqueId}/target/{targetNo}")]
        public async Task<ActionResult<GetTargetResponse>> GetTargetAsync(Guid fixtureUniqueId, int targetNo)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Updates the shots for a specific target in a fixture. This action allows the spotter to modify the recorded shots for a target (also used for first capture).
        /// </summary>
        /// <param name="fixtureUniqueId">The unique identifier of the fixture.</param>
        /// <param name="targetNo">The number of the target.</param>
        /// <param name="request">The request containing the updated shot information.</param>
        /// <returns>The updated target response.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureUniqueId}/target/{targetNo}")]
        public async Task<ActionResult<GetTargetResponse>> UpdateTargetAsync(Guid fixtureUniqueId, int targetNo, UpdateTargetRequest request)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Confirms the current set score for a target in a fixture. This action is typically used to finalize the score after all shots have been recorded and verified. Set points will be awarded after scores have been confirmed. Spotters cannot change a confirmed score.
        /// </summary>
        /// <param name="fixtureUniqueId">The unique identifier of the fixture.</param>
        /// <param name="targetNo">The number of the target.</param>
        /// <returns>The updated target response.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureUniqueId}/target/{targetNo}/confirm")]
        public async Task<ActionResult<GetTargetResponse>> ConfirmCurrentSetScoreAsync(Guid fixtureUniqueId, int targetNo)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Represents the response for retrieving a target's details, including its number, team name, and shots. This class is used to encapsulate the information returned by the GetTargetAsync action.
        /// </summary>
        public class GetTargetResponse : TargetBase
        {
            /// <summary>
            /// Target number
            /// </summary>
            public required int TargetNo { get; set; }

            /// <summary>
            /// Team name
            /// </summary>
            public required string TeamName { get; set; }

            /// <summary>
            /// The current set score of the team in the match. This is the total score obtained by the team in the current set.    
            /// </summary>
            public int? CurrentSetScore { get; set; }

            /// <summary>
            /// Indicates whether the current set score has been confirmed. Once confirmed, the score is finalized and cannot be changed by spotters.
            /// </summary>
            public required bool IsConfirmed { get; set; }
        }

        /// <summary>
        /// Represents the request for updating a target's shots. This class is used to encapsulate the information sent to the UpdateTargetAsync action when modifying the recorded shots for a target.
        /// </summary>
        public class UpdateTargetRequest : TargetBase
        {
        }


        /// <summary>
        /// Represents the base class for target-related information, including the shots. This class is used as a common base for both GetTargetResponse and UpdateTargetRequest.
        /// </summary>
        public abstract class TargetBase
        {

            /// <summary>
            /// Shot values obtained by the team in the current set. 10 is encoded as "+" and "M" is encoded as "0". All other shot values are encoded as their respective integer values. For example, a shot value of 9 is encoded as "9". The shot values are concatenated into a single string. For example, if the team has shot 10, M, and 8 in the current set, the Shots property would be set to "+08".
            /// </summary>
            public required string Shots { get; set; }
        }
    }
}
