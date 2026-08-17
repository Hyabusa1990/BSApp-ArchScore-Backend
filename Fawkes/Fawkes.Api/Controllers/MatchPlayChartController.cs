using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{

    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class MatchPlayChartController : ControllerBase
    {

        /// <summary>
        /// Gets the match play chart for a given fixture.
        /// </summary>
        /// <param name="fixtureId">The unique identifier of the fixture.</param>
        /// <returns>The match play chart response.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("{fixtureId}")]
        public async Task<ActionResult<GetMatchPlayChartResponse>> GetMatchPlayChartAsync(int fixtureId)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Creates targets assignments for a given fixture. Overrides any existing match play chart for the fixture (see hard override attribute in request parameters).
        /// </summary>
        /// <param name="fixtureId">The unique identifier of the fixture.</param>
        /// <param name="request">The request containing the match play chart details.</param>
        /// <returns>The created match play chart response.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost("{fixtureId}")]
        public async Task<ActionResult<GetMatchPlayChartResponse>> CreateMatchPlayChartAsync(int fixtureId, CreateMatchPlayChartRequest request)
        {
            throw new NotImplementedException();
        }



        public class GetMatchPlayChartResponse : MatchPlayChartBase
        {
            public int FixtureId { get; set; }
        }

        public class CreateMatchPlayChartRequest : MatchPlayChartBase
        {
            /// <summary>
            /// If true, the match play chart will be created even if some of the matches already contain data. If false or null, the request will fail if any of the matches already contain data.
            /// </summary>
            public bool? HardOverride { get; set; }
        }
        

        public abstract class MatchPlayChartBase
        {
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
            /// Points scored by the team before the current fixture.
            /// </summary>
            public int SetPoints { get; set; }

            /// <summary>
            /// Total match points/wins accumulated by the team before the current fixture.
            /// </summary>
            public int MatchPoints { get; set; }
        }
    }
}
