using Fawkes.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
            /// <param name="services"></param>
            public void AddFawkesAuthentication(IConfiguration configuration)
            {
                services.AddDbContextPool<IdentityDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("FawkesConnection"));
                });


                services.AddIdentityCore<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                }).AddEntityFrameworkStores<IdentityDbContext>()
                  .AddDefaultTokenProviders();

                services.AddHttpContextAccessor();
                services.AddScoped<ITokenService, TokenService>();
                services.AddScoped<SignInManager<IdentityUser>>();
                services.AddScoped<EmailService>();

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
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
                    };
                });
            }
        }
    }
}
