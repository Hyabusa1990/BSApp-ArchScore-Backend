using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{
    [ApiController]
    public class DosController
    {


        /// <summary>
        /// Gets the current phase of the fixture.
        /// </summary>
        /// <param name="fixtureId">The id of the fixture.</param>
        /// <returns>The current phase of the fixture.</returns>
        /// <exception cref="NotImplementedException"></exception>

        [HttpGet("fixtures/{fixtureId}/phase")]
        public async Task<ActionResult<GetPhaseResponse>> GetPhase(int fixtureId)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Sets the current phase of the fixture. The current phase controls which information is displayed and also where shots captured by the spotters are recorded.
        /// </summary>
        /// <param name="fixtureId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("fixtures/{fixtureId}/phase")]
        public async Task<ActionResult> SetPhase(int fixtureId, SetPhaseRequest request)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets the details of a specific round in a fixture. This action retrieves information about the specified round.
        /// </summary>
        /// <param name="fixtureId">The id of the fixture.</param>
        /// <param name="roundNo">The number of the round to retrieve.</param>
        /// <returns>The details of the specified round.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("fixtures/{fixtureId}/rounds/{roundNo}")]
        public async Task<ActionResult<GetRoundResponse>> GetRoundAsync(int fixtureId, int roundNo)
        {
            throw new NotImplementedException();
        }


        [HttpPut("fixtures/{fixtureId}/rounds/{roundNo}/targets/{targetNo}/sets/{setNo}/score")]
        public async Task<ActionResult> UpdateSetScoreAsync(int fixtureId, int roundNo, int targetNo, int setNo, UpdateSetScoreRequest request)
        {
            throw new NotImplementedException();
        }



        public class GetPhaseResponse : PhaseBase
        {
            /// <summary>
            /// The id of the fixture.
            /// </summary>
            public required int FixtureId { get; set; }
        }


        public class SetPhaseRequest : PhaseBase
        {
        }





        public abstract class PhaseBase
        {
            /// <summary>
            /// The number of the round. 
            /// </summary>
            public required int RoundNo { get; set; } = 0;
            /// <summary>
            /// The number of the set within the round.
            /// </summary>
            public required int SetNo { get; set; } = 0;
        }




        public class GetRoundResponse
        {
            /// <summary>
            /// The id of the fixture.
            /// </summary>
            public required int FixtureId { get; set; }


            /// <summary>
            /// The number of the round. This identifies which round's details are being retrieved.
            /// </summary>
            public required int RoundNo { get; set; }

            /// <summary>
            /// The targets in the round. Each target contains information about the team, total set points, and set scores.
            /// </summary>
            public required TargetInformation[] Targets { get; set; }
        }

        public class TargetInformation
        {
            /// <summary>
            /// The target number. This identifies the specific target within the round.
            /// </summary>
            public required int TargetNo { get; set; }
            /// <summary>
            /// The name of the team associated with the target. This provides context about which team is competing on this target.
            /// </summary>
            public required string TeamName { get; set; }
            /// <summary>
            /// The total set points obtained by the team on this target.
            /// </summary>
            public required int TotalSetPoints { get; set; }
            /// <summary>
            /// The set scores for the team on this target.
            /// </summary>
            public required int[] SetScores { get; set; }
        }

        public class UpdateSetScoreRequest
        {
            /// <summary>
            /// The score to set for the specified set. This is the new score that will be recorded for the set.
            /// </summary>
            public required int Score { get; set; }

            /// <summary>
            /// Indicates whether the score has been confirmed. If true, set points will be awarded to the team based on the score. If false, the score is still provisional and may be subject to change.
            /// </summary>
            public required bool Confirmed { get; set; }



        }
    }
}
