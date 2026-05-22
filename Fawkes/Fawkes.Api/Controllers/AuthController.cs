using Microsoft.AspNetCore.Mvc;

namespace Fawkes.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> PostLoginAsync(LoginRequest loginRequest)
        {
            throw new NotImplementedException();
        }

        [HttpPost("register")]
        public async Task<ActionResult<MessageResponse>> PostRegisterAsync(RegisterRequest registerRequest)
        {
            throw new NotImplementedException();
        }

        [HttpPost("register/{token}")]
        public async Task<ActionResult<MessageResponse>> PostVerifyEmailAsync(string token)
        {
            throw new NotImplementedException();
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponse>> PostRefreshAsync(RefreshRequest refreshRequest)
        {
            throw new NotImplementedException();
        }

        [HttpPost("logout")]
        public async Task<ActionResult> PostLogoutAsync()
        {
            throw new NotImplementedException();
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserResponse>> GetMeAsync()
        {
            throw new NotImplementedException();
        }





        public class LoginRequest
        {
            public required string Email { get; set; }
            public required string Password { get; set; }
        }

        public class TokenResponse
        {
            public required string AccessToken { get; set; }
            public required string RefreshToken { get; set; }
            public int ExpiresIn { get; set; }

        }

        public class RegisterRequest
        {
            public required string Email { get; set; }
            public required string Password { get; set; }
        }

        public class MessageResponse
        {
            public required string Code { get; set; }
            public required string Message { get; set; }
        }

        public class RefreshRequest
        {
            public required string RefreshToken { get; set; }
        }

        public class UserResponse
        {
            public required Guid Id { get; set; }
            public required string Email { get; set; }
        }


    }
}
