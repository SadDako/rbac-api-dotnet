using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Rbac.Application.Behaviors;
using Rbac.Application.Interfaces;
using Rbac.Infrastructure.Authorization;
using Rbac.Infrastructure.Data;
using Rbac.Infrastructure.Repositories;
using Rbac.Infrastructure.Services;
using Rbac.Shared.Options;
using System.Text;
using StackExchange.Redis;

namespace Rbac.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRbacApplication(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ValidationBehavior<,>).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDeviceFingerprintRepository, DeviceFingerprintRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
        services.AddScoped<IPermissionCache, PermissionCacheService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IThreatCacheService, ThreatCacheService>();
        services.AddScoped<IDeviceFingerprintService, DeviceFingerprintService>();
        services.AddScoped<ISecurityEventService, SecurityEventService>();
        services.AddScoped<IGeoLocationService, GeoLocationService>();
        services.AddScoped<IBruteForceProtectionService, BruteForceProtectionService>();
        services.AddScoped<IRiskEngine, RiskEngine>();
        services.AddSingleton<ISecurityMetrics, SecurityMetrics>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IClaimsTransformation, PermissionClaimsTransformer>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IMfaService, Rbac.Infrastructure.Services.MfaService>();

        return services;
    }

    public static IServiceCollection AddRbacInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection is required.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, sql => sql.EnableRetryOnFailure()));

        var redisConnection = configuration.GetValue<string>("Redis:ConnectionString");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddHttpClient("GeoLocation", client =>
        {
            client.BaseAddress = new Uri("https://ipwho.is/");
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        return services;
    }

    public static IServiceCollection AddRbacAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
        {
            throw new InvalidOperationException("JWT key must be configured with at least 32 bytes.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = key,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = "sub",
                    ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
        });

        return services;
    }
}
