using Microsoft.AspNetCore.Identity;

namespace Fawkes.Api.Authentication
{
    public interface ITokenService
    {
        string GenerateAccessToken(IdentityUser user);
        string GenerateAccessTokenForDevice(string deviceCode);
        string GenerateRefreshToken();
        Task<string?> ValidateRefreshTokenAsync(string refreshToken);
    }
}
