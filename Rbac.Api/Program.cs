using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rbac.Api.Application.Activity;
using Rbac.Api.Application.Audit;
using Rbac.Api.Application.Auth;
using Rbac.Api.Application.Authorization;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Infrastructure.Http;
using Rbac.Api.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var (code, message) = ApiProblemDetailsFactory.ResolveDefault(StatusCodes.Status400BadRequest);
        var payload = ApiProblemDetailsFactory.Create(context.HttpContext, StatusCodes.Status400BadRequest, code, message);
        payload.Extensions["errors"] = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());

        var result = new ObjectResult(payload)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };

        result.ContentTypes.Add("application/problem+json");
        return result;
    };
});

builder.Services.AddScoped<Rbac.Api.Application.Auth.AuthService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IActivityStore, InMemoryActivityStore>();
builder.Services.AddScoped<IAuditService, AuditService>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Key)
    || string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("JWT settings are missing (Key/Issuer/Audience).");
}

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException("JWT key must be at least 32 bytes for HS256.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ApiProblemDetailsFactory.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "auth.unauthorized",
                    "Authentication is required for this resource.",
                    context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                await ApiProblemDetailsFactory.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "rbac.forbidden",
                    "You do not have permission to access this resource.",
                    context.HttpContext.RequestAborted);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Use: Bearer {token}"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode < 400)
    {
        return;
    }

    if (!string.IsNullOrWhiteSpace(response.ContentType))
    {
        return;
    }

    var (code, message) = ApiProblemDetailsFactory.ResolveDefault(response.StatusCode);

    await ApiProblemDetailsFactory.WriteAsync(
        context.HttpContext,
        response.StatusCode,
        code,
        message,
        context.HttpContext.RequestAborted);
});

app.UseCors("dev");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await SeedRbacAsync(dbContext);

    if (app.Environment.IsDevelopment())
    {
        await SeedDefaultAdminAsync(dbContext);
    }
}

app.MapGet("/health", (HttpContext context) => Results.Ok(new
{
    status = "ok",
    traceId = context.TraceIdentifier,
    correlationId = context.GetCorrelationId()
}));

app.Run();

static async Task SeedRbacAsync(AppDbContext dbContext)
{
    var roleNames = new[] { "Admin", "User" };

    var existingRoles = await dbContext.Roles
        .Select(role => role.Name)
        .ToListAsync();

    var missingRoles = roleNames.Except(existingRoles).ToList();

    foreach (var roleName in missingRoles)
    {
        dbContext.Roles.Add(new Role { Name = roleName });
    }

    var permissionSeeds = new[]
    {
        new Permission { Key = "admin.access", Description = "Access admin endpoints" },
        new Permission { Key = "users.me.read", Description = "Read own profile" },
        new Permission { Key = "users.read", Description = "List users" },
        new Permission { Key = "users.roles.assign", Description = "Assign role to user" },
        new Permission { Key = "users.roles.remove", Description = "Remove role from user" },
        new Permission { Key = "roles.read", Description = "List roles" },
        new Permission { Key = "roles.create", Description = "Create role" },
        new Permission { Key = "roles.update", Description = "Update role" },
        new Permission { Key = "roles.delete", Description = "Delete role" },
        new Permission { Key = "roles.permissions.update", Description = "Update role permissions" },
        new Permission { Key = "permissions.read", Description = "List permissions" },
        new Permission { Key = "activity.read", Description = "Read activity feed" },
        new Permission { Key = "activity.write", Description = "Write activity feed" }
    };

    var existingPermissionKeys = await dbContext.Permissions
        .Select(permission => permission.Key)
        .ToListAsync();

    var toInsert = permissionSeeds
        .Where(seed => !existingPermissionKeys.Contains(seed.Key))
        .ToList();

    if (missingRoles.Count > 0 || toInsert.Count > 0)
    {
        dbContext.Permissions.AddRange(toInsert);
        await dbContext.SaveChangesAsync();
    }

    var adminRole = await dbContext.Roles.FirstAsync(role => role.Name == "Admin");
    var userRole = await dbContext.Roles.FirstAsync(role => role.Name == "User");

    var permissionMap = await dbContext.Permissions
        .ToDictionaryAsync(permission => permission.Key, permission => permission.Id);

    var adminPermissions = permissionMap.Keys.ToArray();
    var userPermissions = new[]
    {
        "users.me.read",
        "activity.read",
        "activity.write"
    };

    await EnsureRolePermissionsAsync(dbContext, adminRole.Id, adminPermissions, permissionMap);
    await EnsureRolePermissionsAsync(dbContext, userRole.Id, userPermissions, permissionMap);
}

static async Task EnsureRolePermissionsAsync(
    AppDbContext dbContext,
    Guid roleId,
    IReadOnlyCollection<string> requiredPermissionKeys,
    IReadOnlyDictionary<string, Guid> permissionMap)
{
    var existing = await dbContext.RolePermissions
        .Where(rolePermission => rolePermission.RoleId == roleId)
        .Select(rolePermission => rolePermission.PermissionId)
        .ToListAsync();

    var missingPermissionIds = requiredPermissionKeys
        .Where(permissionMap.ContainsKey)
        .Select(key => permissionMap[key])
        .Where(permissionId => !existing.Contains(permissionId))
        .ToList();

    if (missingPermissionIds.Count == 0)
    {
        return;
    }

    foreach (var permissionId in missingPermissionIds)
    {
        dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        });
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedDefaultAdminAsync(AppDbContext dbContext)
{
    const string adminEmail = "admin@rbac.local";
    const string adminName = "Admin";
    const string adminPassword = "Admin@123";

    var adminRole = await dbContext.Roles.FirstAsync(r => r.Name == "Admin");

    var adminUser = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Email == adminEmail);

    if (adminUser is null)
    {
        adminUser = new User
        {
            Name = adminName,
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword)
        };

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();
    }

    var alreadyAdmin = await dbContext.UserRoles
        .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);

    if (!alreadyAdmin)
    {
        dbContext.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await dbContext.SaveChangesAsync();
    }
}

public partial class Program
{
}
