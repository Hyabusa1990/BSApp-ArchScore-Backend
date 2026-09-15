using Fawkes.Api.Core.Model;
using Fawkes.Api.Core.Services;
using Fawkes.Api.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{





    /// <summary>
    /// Controller for managing fixtures in the Fawkes API. Provides endpoints for creating, retrieving, updating, and deleting fixtures.
    /// </summary>

    [Route("fixtures")]
    [ApiController]
    [Authorize]
    public class FixturesController(IFixtureService fixtureService) : ControllerBase
    {

        /// <summary>
        /// Retrieves a fixture by its id. Returns a GetFixtureResponse object containing the fixture details.
        /// </summary>
        /// <param name="id">The id of the fixture to retrieve.</param>
        /// <returns>A GetFixtureResponse object containing the fixture details.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("{id}")]
        public async Task<ActionResult<GetFixtureResponse>> GetFixtureAsync(int id)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }


            var fixture = await fixtureService.GetFixtureAsync(id, User.Identity.Name);

            if (fixture == null)
            {
                return NotFound();
            }
            return ConvertToRepsonse(fixture);

        }

        private static GetFixtureResponse ConvertToRepsonse(Fixture fixture)
        {
            return new GetFixtureResponse()
            {
                Id = fixture.Id,
                UniqueId = fixture.UniqueId,
                Date = fixture.Date,
                FixtureName = fixture.FixtureName ?? string.Empty,
                LeagueName = fixture.LeagueName ?? string.Empty,
                Location = fixture.Location ?? string.Empty

            };
        }


        /// <summary>
        /// Retrieves a list of all fixtures available to the current user.
        /// </summary>
        /// <returns>A list of GetFixtureResponse objects containing the fixture details.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetFixtureResponse>>> GetFixturesAsync()
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            var fixtures = await fixtureService.GetFixturesForUserAsync(User?.Identity?.Name ?? string.Empty);
            return fixtures.Select(f => ConvertToRepsonse(f)).ToList();
        }

        /// <summary>
        /// Creates a new fixture based on the provided CreateFixtureRequest object. Returns a GetFixtureResponse object containing the details of the newly created fixture.
        /// </summary>
        /// <param name="request">The CreateFixtureRequest object containing the details of the fixture to create.</param>
        /// <returns>A GetFixtureResponse object containing the details of the newly created fixture.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        public async Task<ActionResult<GetFixtureResponse>> CreateFixtureAsync(CreateFixtureRequest request)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            var fixture = await fixtureService.CreateFixtureAsync(request.Date, request.Location, request.LeagueName, request.FixtureName, User.Identity.Name);

            return ConvertToRepsonse(fixture);
        }

        /// <summary>
        /// Updates an existing fixture identified by its id based on the provided UpdateFixtureRequest object. Returns a GetFixtureResponse object containing the updated fixture details.
        /// </summary>
        /// <param name="id">The id of the fixture to update.</param>
        /// <param name="request">The UpdateFixtureRequest object containing the updated fixture details.</param>
        /// <returns>A GetFixtureResponse object containing the updated fixture details.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("{id}")]
        public async Task<ActionResult<GetFixtureResponse>> UpdateFixtureAsync(int id, UpdateFixtureRequest request)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            try
            {
                var fixture = await fixtureService.UpdateFixtureAsync(id, request.Date, request.Location, request.LeagueName, request.FixtureName, User.Identity.Name);
                return ConvertToRepsonse(fixture);
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
        /// Deletes an existing fixture identified by its id.
        /// </summary>
        /// <param name="id">The id of the fixture to delete.</param>
        /// <returns>An ActionResult indicating the result of the delete operation.</returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteFixtureAsync(int id)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            try
            {
                await fixtureService.DeleteFixtureAsync(id, User?.Identity?.Name ?? string.Empty);
                return NoContent();
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


        [HttpGet("{id}/users")]
        public async Task<ActionResult<IEnumerable<GetUserResponse>>> GetUsersAsync(int id)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            try
            {
                var users = await fixtureService.GetUsersForFixtureAsync(id, User.Identity.Name);
                return users.Select(u => new GetUserResponse
                {
                    UserName = u.UserName,
                    IsOwner = u.AccessLevel == AccessLevel.Owner
                }).ToList();
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

        [HttpPost("{id}/users/add")]
        public async Task<ActionResult> AddUserAsync(int id, AddUserRequest request)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            try
            {
                await fixtureService.AddUserToFixtureAsync(id, request.UserName, User.Identity.Name);
                return NoContent();
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

        [HttpDelete("{id}/users/{userName}")]
        public async Task<ActionResult> RemoveUserAsync(int id, string userName)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(User.Identity.Name))
            {
                return Unauthorized();
            }

            try
            {
                await fixtureService.RemoveUserFromFixtureAsync(id, userName, User.Identity.Name);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
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
        /// Contains all details of a fixture including its id and unique identifier.
        /// </summary>
        public class GetFixtureResponse : FixtureBase
        {
            /// <summary>
            /// Id under which the fixture resource is stored. This is used for all CRUD operations on the fixture resource.
            /// </summary>
            public required int Id { get; set; }

            /// <summary>
            /// Globally unique identifier of fixture resource. This is considered "hard to guess" and used for operations on the fixture that don't require authentication (e.g. for the spotter to submit scores).
            /// </summary>
            public required Guid UniqueId { get; set; }
        }

        public class CreateFixtureRequest : FixtureBase
        {
        }

        public class UpdateFixtureRequest : FixtureBase
        {
        }

        public abstract class FixtureBase
        {
            /// <summary>
            /// Date of the fixture in UTC. This property is required and must be provided when creating or updating a fixture.
            /// </summary>
            public required DateTime Date { get; set; }

            /// <summary>
            /// Location of the fixture. This property is required and must be provided when creating or updating a fixture.
            /// </summary>
            public required string Location { get; set; }

            /// <summary>
            /// Name of the league for the fixture. This property is required and must be provided when creating or updating a fixture. Typical values might be "1. Bundesliga Nord" or "Regionalliga Südwest"
            /// </summary>
            public required string LeagueName { get; set; }

            /// <summary>
            /// Name of the fixture. This property is required and must be provided when creating or updating a fixture. Typical values might be "3. Wettkampftag", "Relegation" or "Finale".
            /// </summary>
            public required string FixtureName { get; set; }
        }

        public class AddUserRequest : UserBase
        {

        }

        public class GetUserResponse : UserBase
        {
            /// <summary>
            /// Indicates whether the user being added should have owner-level permissions for the fixture. If set to true, the user will have full control over the fixture, including the ability to manage other users and modify fixture details. If set to false or null, the user will have standard access permissions.
            /// </summary>
            public bool IsOwner { get; set; }

        }

        public abstract class UserBase
        {
            /// <summary>
            /// User name of the user to add to the fixture. This property is required and must be provided when adding a user to a fixture.
            /// </summary>
            public required string UserName { get; set; }

        }
    }
}
