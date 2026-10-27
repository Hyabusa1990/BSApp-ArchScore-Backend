using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Fawkes.Api.Authentication
{
    [TestFixture]
    public class TokenServiceTests
    {
        private const string UserKey = "test-user-signing-key-0123456789-0123456789-0123456789";
        private const string DeviceKey = "test-device-signing-key-0123456789-0123456789-01234567";

        private TokenService tokenService;

        [SetUp]
        public void Setup()
        {
            tokenService = new TokenService(CreateConfiguration(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = UserKey,
                ["Jwt:Issuer"] = "Fawkes.Api",
                ["Jwt:Audience"] = "Fawkes.Client",
                ["Jwt:ExpirationMinutes"] = "60",
                ["JwtDevice:Key"] = DeviceKey,
                ["JwtDevice:Issuer"] = "Fawkes.Api",
                ["JwtDevice:Audience"] = "Fawkes.Client",
                ["JwtDevice:ExpirationMinutes"] = "7200"
            }));
        }

        private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
            => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        [Test]
        public void GenerateAccessTokenContainsUserClaims()
        {
            var user = new IdentityUser() { Id = "user-1", Email = "archer@example.org" };

            var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenService.GenerateAccessToken(user));

            Assert.That(token.Issuer, Is.EqualTo("Fawkes.Api"));
            Assert.That(token.Audiences, Is.EqualTo(new[] { "Fawkes.Client" }));
            Assert.That(token.Subject, Is.EqualTo("user-1"));
            Assert.That(token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value, Is.EqualTo("archer@example.org"));
            Assert.That(token.ValidTo, Is.EqualTo(DateTime.UtcNow.AddMinutes(60)).Within(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void GenerateAccessTokenForDeviceIsSignedWithDeviceKey()
        {
            var tokenString = tokenService.GenerateAccessTokenForDevice("123456");

            new JwtSecurityTokenHandler().ValidateToken(tokenString, new TokenValidationParameters()
            {
                ValidIssuer = "Fawkes.Api",
                ValidAudience = "Fawkes.Client",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DeviceKey))
            }, out var validatedToken);

            var token = (JwtSecurityToken)validatedToken;
            Assert.That(token.Claims.First(c => c.Type == "device_code").Value, Is.EqualTo("123456"));
            Assert.That(token.ValidTo, Is.EqualTo(DateTime.UtcNow.AddMinutes(7200)).Within(TimeSpan.FromMinutes(1)));
        }

        [Test]
        public void GenerateAccessTokenFailsWithoutKey()
        {
            var service = new TokenService(CreateConfiguration(new Dictionary<string, string?>()));

            Assert.Throws<InvalidOperationException>(() => service.GenerateAccessToken(new IdentityUser() { Id = "user-1" }));
            Assert.Throws<InvalidOperationException>(() => service.GenerateAccessTokenForDevice("123456"));
        }

        [Test]
        public void GenerateRefreshTokenReturnsRandom32Bytes()
        {
            var first = tokenService.GenerateRefreshToken();
            var second = tokenService.GenerateRefreshToken();

            Assert.That(Convert.FromBase64String(first), Has.Length.EqualTo(32));
            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public async Task StoredRefreshTokenResolvesToUser()
        {
            tokenService.StoreRefreshToken("refresh", "user-1", DateTime.UtcNow.AddDays(1));

            Assert.That(await tokenService.ValidateRefreshTokenAsync("refresh"), Is.EqualTo("user-1"));
        }

        [Test]
        public async Task UnknownRefreshTokenIsRejected()
        {
            Assert.That(await tokenService.ValidateRefreshTokenAsync("unknown"), Is.Null);
        }

        [Test]
        public async Task ExpiredRefreshTokenIsRejected()
        {
            tokenService.StoreRefreshToken("refresh", "user-1", DateTime.UtcNow.AddMinutes(-1));

            Assert.That(await tokenService.ValidateRefreshTokenAsync("refresh"), Is.Null);
        }

        [Test]
        public async Task RevokedRefreshTokenIsRejected()
        {
            tokenService.StoreRefreshToken("refresh", "user-1", DateTime.UtcNow.AddDays(1));
            tokenService.RevokeRefreshToken("refresh");

            Assert.That(await tokenService.ValidateRefreshTokenAsync("refresh"), Is.Null);
        }
    }
}
