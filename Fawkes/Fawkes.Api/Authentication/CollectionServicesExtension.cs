using Fawkes.Api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Fawkes.Api.Authentication
{
    public static class CollectionServicesExtension
    {
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Injects Fawkes authentication services into the provided IServiceCollection.
            /// </summary>
            public void AddFawkesAuthentication(IConfiguration configuration)
            {
                services.AddDbContextPool<IdentityDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("FawkesConnection"));
                });


                services.AddIdentityCore<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedEmail = false;
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Email;
                }).AddEntityFrameworkStores<IdentityDbContext>()
                  .AddDefaultTokenProviders();

                services.AddHttpContextAccessor();
                services.AddSingleton<ITokenService, TokenService>();
                services.AddScoped<SignInManager<IdentityUser>>();
                services.AddScoped<EmailService>();

                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        // User-Tokens (Jwt:*) und Display-Geräte-Tokens (JwtDevice:*) werden mit
                        // getrennter Konfiguration signiert — beide müssen akzeptiert werden, sonst
                        // bekommen Displays 401, sobald die Werte voneinander abweichen.
                        ValidIssuers = new[] { configuration["Jwt:Issuer"], configuration["JwtDevice:Issuer"] }
                            .Where(v => !string.IsNullOrEmpty(v)).Distinct().ToArray(),
                        ValidAudiences = new[] { configuration["Jwt:Audience"], configuration["JwtDevice:Audience"] }
                            .Where(v => !string.IsNullOrEmpty(v)).Distinct().ToArray(),
                        IssuerSigningKeys = new[] { configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"), configuration["JwtDevice:Key"] }
                            .Where(v => !string.IsNullOrEmpty(v)).Distinct()
                            .Select(k => (SecurityKey)new SymmetricSecurityKey(Encoding.UTF8.GetBytes(k!))).ToArray()
                    };
                });
            }
        }
    }
}
