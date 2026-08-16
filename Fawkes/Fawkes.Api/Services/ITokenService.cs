using Microsoft.AspNetCore.Identity;

namespace Fawkes.Api.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(IdentityUser user);
        string GenerateRefreshToken();
        Task<string?> ValidateRefreshTokenAsync(string refreshToken);
    }
}
