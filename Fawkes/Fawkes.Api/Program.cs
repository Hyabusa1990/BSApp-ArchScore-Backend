using Fawkes.Api.Authentication;
using Fawkes.Api.Core;
using Fawkes.Api.Filters;
using Fawkes.Api.Store;
using Fawkes.Api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUtils();
builder.Services.AddFawkesAuthentication(builder.Configuration);
builder.Services.AddFawkesDataLayer(builder.Configuration);
builder.Services.AddApplicationLogic();


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

    config.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Fawkes API",
        Version = "v1",
        Description = "API for Fawkes application",
        Contact = new Microsoft.OpenApi.OpenApiContact
        {
            Name = "Dominik Schindler",
            Email = "dominik.schindler@gmx.de"
        }
    });

    // Add JWT Bearer authorization
    config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    // Apply security only to endpoints with [Authorize] attribute
    config.OperationFilter<AuthorizeOperationFilter>();

    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    config.IncludeXmlComments(xmlPath);

});







var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/status", () => "OK");
// Basisversion aus appsettings ("version"), dazu die Commit-SHA des Builds (per --build-arg
// GIT_SHA beim Image-Build gesetzt, siehe Dockerfile/Workflow).
var buildSha = Environment.GetEnvironmentVariable("BUILD_SHA");
var baseVersion = builder.Configuration["version"] ?? "unknown";
var versionText = string.IsNullOrWhiteSpace(buildSha) || buildSha == "dev"
    ? baseVersion
    : $"{baseVersion} ({buildSha[..Math.Min(7, buildSha.Length)]})";
app.MapGet("/version", () => versionText);


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await services.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        Console.WriteLine("Migrations applied successfully: IdentityDbContext.");
        await services.GetRequiredService<FawkesDbContext>().Database.MigrateAsync();
        Console.WriteLine("Migrations applied successfully: FawkesDbContext.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying migrations.");
        // Optionally stop startup if critical
        // throw;
    }
}

app.Run();
