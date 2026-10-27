using Fawkes.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fawkes.Api.Controllers
{



    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        // Same ProblemDetails shape as the central exception handler: status, title, detail and a machine-readable code.
        private ObjectResult ErrorResult(int statusCode, string code, string detail)
        {
            var problemDetails = ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode, detail: detail);
            problemDetails.Extensions["code"] = code;

            return new ObjectResult(problemDetails)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> PostLoginAsync(LoginRequest loginRequest)
        {
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                _logger.LogWarning("Failed login for {Email}: unknown user", loginRequest.Email);
                return ErrorResult(StatusCodes.Status401Unauthorized, "INVALID_CREDENTIALS", "Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login for user {UserId}: wrong password", user.Id);
                return ErrorResult(StatusCodes.Status401Unauthorized, "INVALID_CREDENTIALS", "Invalid email or password");
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogInformation("Login of user {UserId} refused: email not confirmed", user.Id);
                return ErrorResult(StatusCodes.Status403Forbidden, "EMAIL_NOT_CONFIRMED", "Email not confirmed");
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            if (_tokenService is TokenService tokenService)
            {
                tokenService.StoreRefreshToken(refreshToken, user.Id, DateTime.UtcNow.AddDays(7));
            }

            _logger.LogInformation("User {UserId} ({Email}) logged in", user.Id, user.Email);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<MessageResponse>> PostRegisterAsync(RegisterRequest registerRequest)
        {
            var existingUser = await _userManager.FindByEmailAsync(registerRequest.Email);
            if (existingUser != null)
            {
                return ErrorResult(StatusCodes.Status409Conflict, "EMAIL_EXISTS", "A user with this email already exists");
            }

            var user = new IdentityUser
            {
                UserName = registerRequest.Email,
                Email = registerRequest.Email,
                LockoutEnabled = false,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);
            if (!result.Succeeded)
            {
                _logger.LogInformation("Registration for {Email} rejected: {Errors}", registerRequest.Email, string.Join(", ", result.Errors.Select(e => e.Code)));
                return ErrorResult(StatusCodes.Status400BadRequest, "REGISTRATION_FAILED", string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Registered new user {UserId} ({Email})", user.Id, user.Email);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            return Ok(new MessageResponse
            {
                Code = "REGISTRATION_SUCCESS",
                Message = $"Registration successful. Please verify your email using the token: {token}"
            });
        }

        [HttpPost("register/{token}")]
        public async Task<ActionResult<MessageResponse>> PostVerifyEmailAsync(string token)
        {
            var email = Request.Query["email"].ToString();
            if (string.IsNullOrEmpty(email))
            {
                return ErrorResult(StatusCodes.Status400BadRequest, "INVALID_REQUEST", "Email is required");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return ErrorResult(StatusCodes.Status404NotFound, "USER_NOT_FOUND", "User not found");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return ErrorResult(StatusCodes.Status400BadRequest, "VERIFICATION_FAILED", "Email verification failed");
            }

            _logger.LogInformation("User {UserId} confirmed their email address", user.Id);

            return Ok(new MessageResponse
            {
                Code = "VERIFICATION_SUCCESS",
                Message = "Email verified successfully"
            });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponse>> PostRefreshAsync(RefreshRequest refreshRequest)
        {
            var userId = await _tokenService.ValidateRefreshTokenAsync(refreshRequest.RefreshToken);
            if (userId == null)
            {
                // Refresh tokens are kept in memory, so every API restart invalidates them; not a security event by itself.
                _logger.LogInformation("Token refresh rejected: unknown or expired refresh token");
                return ErrorResult(StatusCodes.Status401Unauthorized, "INVALID_REFRESH_TOKEN", "Invalid refresh token");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ErrorResult(StatusCodes.Status401Unauthorized, "INVALID_REFRESH_TOKEN", "The user of this refresh token no longer exists");
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            if (_tokenService is TokenService tokenService)
            {
                tokenService.RevokeRefreshToken(refreshRequest.RefreshToken);
                tokenService.StoreRefreshToken(newRefreshToken, user.Id, DateTime.UtcNow.AddDays(7));
            }

            _logger.LogDebug("Refreshed tokens for user {UserId}", user.Id);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 3600
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> PostLogoutAsync()
        {
            var refreshToken = Request.Headers["X-Refresh-Token"].ToString();
            if (!string.IsNullOrEmpty(refreshToken) && _tokenService is TokenService tokenService)
            {
                tokenService.RevokeRefreshToken(refreshToken);
            }

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {UserName} logged out", User.Identity?.Name);

            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserResponse>> GetMeAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserResponse
            {
                Id = Guid.Parse(user.Id),
                Email = user.Email ?? string.Empty
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<MessageResponse>> PostChangePasswordAsync(ChangePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return ErrorResult(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "User not authenticated");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ErrorResult(StatusCodes.Status404NotFound, "USER_NOT_FOUND", "User not found");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogInformation("Password change of user {UserId} rejected: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Code)));
                return ErrorResult(StatusCodes.Status400BadRequest, "PASSWORD_CHANGE_FAILED", string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("User {UserId} changed their password", user.Id);

            return Ok(new MessageResponse
            {
                Code = "PASSWORD_CHANGED",
                Message = "Password changed successfully"
            });
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

        public class ChangePasswordRequest
        {
            public required string CurrentPassword { get; set; }
            public required string NewPassword { get; set; }
        }


    }
}
