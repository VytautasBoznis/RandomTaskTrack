using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RandomTaskTrack.API.ActionFilters;
using RandomTaskTrack.API.Extensions;
using RandomTaskTrack.Business.Base;
using RandomTaskTrack.Data.Models.ConfigurationOptions;
using RandomTaskTrack.Data.Models.Constants;
using Serilog;

namespace RandomTaskTrack.API;

public class Program
{
    public static void Main(string[] args)
    {
        const string AllowedCorsOrigins = "configuredOrigins";

        var builder = WebApplication.CreateBuilder(args);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        builder.Host.UseSerilog();

        // This API is reachable from the public internet, so CORS is an
        // explicit allow-list rather than "*". Set Cors__AllowedOrigins__0 to
        // the tablet-facing origin.
        string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(AllowedCorsOrigins, policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                }
                else
                {
                    policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                }
            });
        });

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new ErrorHandlingFilterAttribute());
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer {token}'"
            });
        });

        var jwtOptions = configuration.GetSection(AppSettingKeys.JwtSection).Get<JwtOptions>();

        if (jwtOptions == null || string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
        {
            throw new InvalidOperationException("Jwt configuration is missing or invalid. Set Jwt:SecretKey (env: Jwt__SecretKey).");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = !string.IsNullOrEmpty(jwtOptions.Issuer),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = !string.IsNullOrEmpty(jwtOptions.Audience),
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
            });

        builder.Services.AddAuthorization();

        // Schema is snake_case, models are PascalCase.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Due dates and times are DateOnly/TimeOnly, which Dapper cannot bind on
        // its own. Without these, every query that takes one throws.
        Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        Dapper.SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

        // Keep JWT claim names exactly as issued so our constants match
        // throughout instead of being remapped to ClaimTypes.* URIs.
        System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        builder.Services
            .AddConfig(configuration)
            .RegisterServices()
            .AddRepositories()
            .AddValidators()
            .AddOperations()
            .AddAiServices()
            .AddRecipeServices()
            .AddFinanceServices()
            .AddDomainServices();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.Path.StartsWithSegments("/api") && !ctx.Request.Path.StartsWithSegments("/health"))
            {
                ctx.Response.StatusCode = 404;
                return;
            }

            await next();
        });

        app.UseCors(AllowedCorsOrigins);

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
