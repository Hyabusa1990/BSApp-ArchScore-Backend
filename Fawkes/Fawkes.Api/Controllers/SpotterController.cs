using Azure.Core;
using Fawkes.Api.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{
    [ApiController]
    public class SpotterController(ISpotterService spotterService) : ControllerBase
    {

        /// <summary>
        /// Retrieves the details of a specific target in a fixture. This action is used to get the current state of a target, including its shots and associated team information.
        /// </summary>
        /// <param name="fixtureUniqueId">The unique identifier of the fixture.</param>
        /// <param name="targetNo">The number of the target.</param>
        /// <returns>The target response containing the current state of the target.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("fixtures/{fixtureUniqueId}/targets/{targetNo}/spotter/info")]
        public async Task<ActionResult<GetSpotterTargetInfo>> GetSpotterTargetInfoAsync(Guid fixtureUniqueId, int targetNo)
        {

            try
            {
                var targetData = await spotterService.GetTargetDataAsync(fixtureUniqueId, targetNo);


                return Ok(new GetSpotterTargetInfo
                {
                    TargetNo = targetData.TargetNo,
                    CurrentSetNo = targetData.CurrentSetNo,
                    TeamName = targetData.TeamName,
                    Shots = targetData.Shots,
                    CurrentSetScore = targetData.CurrentSetScore,
                    IsConfirmed = targetData.IsConfirmed
                });
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
        /// Updates the shots for a specific target in a fixture. This action allows the spotter to modify the recorded shots for a target (also used for first capture).
        /// </summary>
        /// <param name="fixtureUniqueId">The unique identifier of the fixture.</param>
        /// <param name="targetNo">The number of the target.</param>
        /// <param name="request">The request containing the updated shot information.</param>
        /// <returns>The updated target response.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureUniqueId}/targets/{targetNo}/spotter/shots")]
        public async Task<ActionResult<GetSpotterTargetInfo>> UpdateTargetAsync(Guid fixtureUniqueId, int targetNo, SubmitShotsRequest request)
        {
            try
            {
                var targetInfo = await spotterService.ProcessShotsAsync(fixtureUniqueId, targetNo, request.Shots);


                return Ok(new GetSpotterTargetInfo
                {
                    TargetNo = targetInfo.TargetNo,
                    CurrentSetNo = targetInfo.CurrentSetNo,
                    TeamName = targetInfo.TeamName,
                    Shots = targetInfo.Shots,
                    CurrentSetScore = targetInfo.CurrentSetScore,
                    IsConfirmed = targetInfo.IsConfirmed
                });
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
        /// Confirms the current set score for a target in a fixture. This action is typically used to finalize the score after all shots have been recorded and verified. Set points will be awarded after scores have been confirmed. Spotters cannot change a confirmed score.
        /// </summary>
        /// <param name="fixtureUniqueId">The unique identifier of the fixture.</param>
        /// <param name="targetNo">The number of the target.</param>
        /// <returns>The updated target response.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureUniqueId}/targets/{targetNo}/spotter/shots/confirm")]
        public async Task<ActionResult<GetSpotterTargetInfo>> ConfirmCurrentSetScoreAsync(Guid fixtureUniqueId, int targetNo)
        {
            try
            {
                var targetInfo = await spotterService.ConfirmEndScoreAsync(fixtureUniqueId, targetNo);


                return Ok(new GetSpotterTargetInfo
                {
                    TargetNo = targetInfo.TargetNo,
                    CurrentSetNo = targetInfo.CurrentSetNo,
                    TeamName = targetInfo.TeamName,
                    Shots = targetInfo.Shots,
                    CurrentSetScore = targetInfo.CurrentSetScore,
                    IsConfirmed = targetInfo.IsConfirmed
                });
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
        /// Represents the response for retrieving a target's details, including its number, team name, and shots. This class is used to encapsulate the information returned by the GetTargetAsync action.
        /// </summary>
        public class GetSpotterTargetInfo : TargetInfoBase
        {
            /// <summary>
            /// Target number
            /// </summary>
            public required int TargetNo { get; set; }

            /// <summary>
            /// The current set number of the match. This indicates which set is currently being played in the match. The current set number is zero, when the match has not yet started or is already over.
            /// </summary>
            public required int CurrentSetNo { get; set; }

            /// <summary>
            /// Team name
            /// </summary>
            public required string TeamName { get; set; }

            /// <summary>
            /// The current set score of the team in the match. This is the total score obtained by the team in the current set.    
            /// </summary>
            public int? CurrentSetScore { get; set; }

            /// <summary>
            /// Indicates whether the current set score has been confirmed. This property is used to determine if the score for the current set has been finalized and cannot be changed. If true, the score is confirmed; if false, the score can still be modified.
            /// </summary>
            public bool IsConfirmed { get; set; }
        }

        /// <summary>
        /// Represents the request for updating a target's shots. This class is used to encapsulate the information sent to the UpdateTargetAsync action when modifying the recorded shots for a target.
        /// </summary>
        public class SubmitShotsRequest : TargetInfoBase
        {
        }


        /// <summary>
        /// Represents the base class for target-related information, including the shots. This class is used as a common base for both GetTargetResponse and UpdateTargetRequest.
        /// </summary>
        public abstract class TargetInfoBase
        {

            /// <summary>
            /// Shot values obtained by the team in the current set. 10 is encoded as "+" and "M" is encoded as "0". All other shot values are encoded as their respective integer values. For example, a shot value of 9 is encoded as "9". The shot values are concatenated into a single string. For example, if the team has shot 10, M, and 8 in the current set, the Shots property would be set to "+08".
            /// </summary>
            public required string Shots { get; set; }
        }
    }
}
