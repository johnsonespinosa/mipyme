using System.Text;
using Application.Common.Data;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal();
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString == "REPLACE_WITH_USER_SECRETS_OR_ENV_VAR")
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Database no está configurado. " +
                "En desarrollo ejecuta: dotnet user-secrets set \"ConnectionStrings:Database\" \"<valor>\" --project src/Presentation.Api/Presentation.Api.csproj " +
                "o define la variable de entorno ConnectionStrings__Database en producción.");
        }

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString == "REPLACE_WITH_USER_SECRETS_OR_ENV_VAR")
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Database no está configurado para health checks. " +
                "En desarrollo ejecuta: dotnet user-secrets set \"ConnectionStrings:Database\" \"<valor>\" --project src/Presentation.Api/Presentation.Api.csproj " +
                "o define la variable de entorno ConnectionStrings__Database en producción.");
        }

        services
            .AddHealthChecks()
            .AddNpgSql(connectionString);

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(jwtSecret) ||
            jwtSecret == "REPLACE_WITH_USER_SECRETS_OR_ENV_VAR")
        {
            throw new InvalidOperationException(
                "Jwt:Secret no está configurado. " +
                "En desarrollo ejecuta: dotnet user-secrets set \"Jwt:Secret\" \"<valor>\" --project src/Presentation.Api/Presentation.Api.csproj " +
                "o define la variable de entorno Jwt__Secret en producción.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();

        return services;
    }

    private static void AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
    }
}