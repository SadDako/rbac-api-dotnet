using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rbac.Api.Extensions;
using Rbac.Infrastructure.Data;
using Rbac.Shared.Options;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRbacInfrastructure(builder.Configuration);
builder.Services.AddRbacApplication();
builder.Services.AddRbacAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Observability: OpenTelemetry, Metrics, Tracing, Prometheus, OTLP
builder.Services.AddObservability(builder.Configuration);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", builder.Environment.ApplicationName ?? "rbac-api")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Use Bearer {token}"
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

builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            if (!allowedOrigins.Any())
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production.");
            }

            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
var trustedProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
foreach (var proxy in trustedProxies)
{
    if (System.Net.IPAddress.TryParse(proxy, out var ip))
    {
        forwardedOptions.KnownProxies.Add(ip);
    }
}
app.UseForwardedHeaders(forwardedOptions);

app.UseCorrelationId();
app.UseMiddleware<Rbac.Api.Middleware.SecurityThreatMiddleware>();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var detail = builder.Environment.IsDevelopment() ? feature?.Error?.Message : "An unexpected error occurred.";

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail
        });
    });
});

app.Use((context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }
    context.Response.Headers["X-XSS-Protection"] = "0";

    var path = context.Request.Path.Value ?? string.Empty;
    var isSwaggerUi = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
    context.Response.Headers["Content-Security-Policy"] = isSwaggerUi
        ? "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; frame-ancestors 'none'; base-uri 'self';"
        : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none';";
    return next(context);
});

app.UseHttpsRedirection();
app.UseCors("ApiCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Audit middleware records requests/responses, exceptions and metadata
app.UseMiddleware<Rbac.Api.Middleware.AuditMiddleware>();

// Expose Prometheus metrics at /metrics
app.MapPrometheusScrapingEndpoint("/metrics");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    await SeedRolesAsync(dbContext);
    await SeedDefaultAdminAsync(dbContext);
}

app.Run();

static async Task SeedRolesAsync(AppDbContext dbContext)
{
    var roleNames = new[] { "Admin", "User" };
    var existingRoles = await dbContext.Roles.Select(r => r.Name).ToListAsync();
    var missingRoles = roleNames.Except(existingRoles).ToList();
    if (!missingRoles.Any())
    {
        return;
    }

    foreach (var roleName in missingRoles)
    {
        dbContext.Roles.Add(new Rbac.Domain.Entities.Role { Name = roleName, DisplayName = roleName });
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedDefaultAdminAsync(AppDbContext dbContext)
{
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        return;
    }

    var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
    var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
    if (adminRole is null)
    {
        adminRole = new Rbac.Domain.Entities.Role { Name = "Admin", DisplayName = "Administrator" };
        dbContext.Roles.Add(adminRole);
        await dbContext.SaveChangesAsync();
    }

    var adminUser = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

    if (adminUser is null)
    {
        adminUser = new Rbac.Domain.Entities.User
        {
            Name = "Admin",
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword)
        };

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();
    }

    var alreadyAdmin = await dbContext.UserRoles
        .Include(ur => ur.Role)
        .AnyAsync(ur => ur.UserId == adminUser.Id && ur.Role.Name == "Admin");

    if (!alreadyAdmin)
    {
        dbContext.UserRoles.Add(new Rbac.Domain.Entities.UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await dbContext.SaveChangesAsync();
    }
}
