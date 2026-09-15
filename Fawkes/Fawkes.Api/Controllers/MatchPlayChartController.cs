using Fawkes.Api.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{

    [Authorize]
    [ApiController]
    public class MatchPlayChartController(IMatchPlayChartService matchPlayChartService) : ControllerBase
    {


        /// <summary>
        /// Creates targets assignments for a given fixture. Overrides any existing match play chart for the fixture (see hard override attribute in request parameters).
        /// </summary>
        /// <param name="fixtureId">The unique identifier of the fixture.</param>
        /// <param name="request">The request containing the match play chart details.</param>
        /// <returns>The created match play chart response.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost("fixtures/{fixtureId}/matchplaychart")]
        public async Task<ActionResult> CreateMatchPlayChartAsync(int fixtureId, CreateMatchPlayChartRequest request)
        {

            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            try
            {
                var teams = request.Teams.Select(t => new Core.Model.Team
                {
                    Name = t.Name,
                    SetPointsWon = t.SetPointsWon,
                    SetPointsLost = t.SetPointsLost,
                    MatchPointsWon = t.MatchPointsWon,
                    MatchPointsLost = t.MatchPointsLost
                }).ToArray();

                await matchPlayChartService.CreateMatchPlayChartAsync(fixtureId, teams, request.TargetAssignments, request.HardOverride ?? false, User.Identity.Name);
                return Ok();
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }




        public class CreateMatchPlayChartRequest
        {
            /// <summary>
            /// If true, the match play chart will be created even if some of the matches already contain data. If false or null, the request will fail if any of the matches already contain data.
            /// </summary>
            public bool? HardOverride { get; set; }

            /// <summary>
            /// List of teams participating in the fixture. Each team should have a unique name. The order of the teams in the array determines their position in the match play chart.
            /// </summary>
            public required Team[] Teams { get; set; }

            /// <summary>
            /// List of target assignments for the fixture. Each inner array represents a round, and the integers within each inner array represent the indices of the teams assigned to that target (starting from 1, 0 indicates an empty target). The order of the teams in each inner array determines their position on the target.
            /// </summary>
            public int[][]? TargetAssignments { get; set; }
        }



        public class Team
        {
            public required string Name { get; set; }

            /// <summary>
            /// Set points won by the team before the current fixture.
            /// </summary>
            public int SetPointsWon { get; set; }
            
            /// <summary>
            /// Set points lost by the team before the current fixture.
            /// </summary>
            public int SetPointsLost { get; set; }

            /// <summary>
            /// Total match points/wins accumulated by the team before the current fixture.
            /// </summary>
            public int MatchPointsWon { get; set; }
            
            /// <summary>
            /// Total match points/wins lost by the team before the current fixture.
            /// </summary>
            public int MatchPointsLost { get; set; }
        }

    }
}
