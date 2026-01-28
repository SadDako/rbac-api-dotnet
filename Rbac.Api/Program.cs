using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Options;
using Rbac.Api.Application.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddScoped<Rbac.Api.Application.Auth.AuthService>();
builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddScoped<AuthService>();

var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Key)
    || string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("JWT settings are missing (Key/Issuer/Audience).");
}

// HS256 exige chave com no mínimo 32 bytes
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

            // Uso sub como "identidade" principal
            NameClaimType = JwtRegisteredClaimNames.Sub,
            // Roles padrão do .NET
            RoleClaimType = ClaimTypes.Role,

            // Evita erro chato de relógio em dev
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

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
        Description = "Cole assim: Bearer {seu_token}"
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

// app.UseHttpsRedirection();

app.UseCors("dev");

app.UseAuthentication();

app.UseAuthorization();

// Map controllers 
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await SeedRolesAsync(dbContext);

    if (app.Environment.IsDevelopment())
    {
        await SeedDefaultAdminAsync(dbContext);
    }
}

// Health check simples
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static async Task SeedRolesAsync(AppDbContext dbContext)
{
    var roleNames = new[] { "Admin", "User" };

    var existingRoles = await dbContext.Roles
        .Select(role => role.Name)
        .ToListAsync();

    var missingRoles = roleNames.Except(existingRoles).ToList();
    if (missingRoles.Count == 0)
    {
        return;
    }

    foreach (var roleName in missingRoles)
    {
        dbContext.Roles.Add(new Role { Name = roleName });
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedDefaultAdminAsync(AppDbContext dbContext)
{
    const string adminEmail = "admin@rbac.local";
    const string adminName = "Admin";
    const string adminPassword = "Admin@123";

    var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
    if (adminRole is null)
    {
        adminRole = new Role { Name = "Admin" };
        dbContext.Roles.Add(adminRole);
        await dbContext.SaveChangesAsync();
    }

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

    // Garante vínculo com role Admin
    var alreadyAdmin = await dbContext.UserRoles
        .Include(ur => ur.Role)
        .AnyAsync(ur => ur.UserId == adminUser.Id && ur.Role.Name == "Admin");

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
