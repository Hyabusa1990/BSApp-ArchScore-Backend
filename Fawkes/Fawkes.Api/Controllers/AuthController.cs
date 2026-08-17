using Fawkes.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fawkes.Api.Controllers
{



    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            EmailService emailService,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> PostLoginAsync(LoginRequest loginRequest)
        {
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.EmailConfirmed)
            {
                return Unauthorized(new { message = "Email not confirmed" });
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            if (_tokenService is TokenService tokenService)
            {
                tokenService.StoreRefreshToken(refreshToken, user.Id, DateTime.UtcNow.AddDays(7));
            }

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
                return BadRequest(new MessageResponse
                {
                    Code = "EMAIL_EXISTS",
                    Message = "A user with this email already exists"
                });
            }

            var user = new IdentityUser
            {
                UserName = registerRequest.Email,
                Email = registerRequest.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new MessageResponse
                {
                    Code = "REGISTRATION_FAILED",
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }

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
                return BadRequest(new MessageResponse
                {
                    Code = "INVALID_REQUEST",
                    Message = "Email is required"
                });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new MessageResponse
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found"
                });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return BadRequest(new MessageResponse
                {
                    Code = "VERIFICATION_FAILED",
                    Message = "Email verification failed"
                });
            }

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
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            if (_tokenService is TokenService tokenService)
            {
                tokenService.RevokeRefreshToken(refreshRequest.RefreshToken);
                tokenService.StoreRefreshToken(newRefreshToken, user.Id, DateTime.UtcNow.AddDays(7));
            }

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
                return Unauthorized(new MessageResponse
                {
                    Code = "UNAUTHORIZED",
                    Message = "User not authenticated"
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new MessageResponse
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found"
                });
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new MessageResponse
                {
                    Code = "PASSWORD_CHANGE_FAILED",
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }

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
