using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Rbac.Api.Tests;

public class AuthAndRbacIntegrationTests : IClassFixture<RbacApiFactory>
{
    private readonly HttpClient _client;

    public AuthAndRbacIntegrationTests(RbacApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsJwtToken()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@rbac.local",
            password = "Admin@123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
    }

    [Fact]
    public async Task Permissions_WithoutPermission_ReturnsForbiddenProblemDetails()
    {
        var email = $"user-{Guid.NewGuid():N}@rbac.local";

        await _client.PostAsJsonAsync("/auth/register", new
        {
            name = "Regular User",
            email,
            password = "User@123"
        });

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "User@123"
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginPayload);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Token);
        var permissionsResponse = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, permissionsResponse.StatusCode);

        var problem = await permissionsResponse.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal("rbac.forbidden", problem!.Code);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
        Assert.False(string.IsNullOrWhiteSpace(problem.CorrelationId));
    }

    [Fact]
    public async Task CreateRole_AsAdmin_ReturnsCreated()
    {
        var adminToken = await LoginAsAdminAsync();
        var roleName = $"Role-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = JsonContent.Create(new { name = roleName });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_AsAdmin_ReturnsOk()
    {
        var adminToken = await LoginAsAdminAsync();
        var roleName = $"Role-{Guid.NewGuid():N}";
        var userEmail = $"assign-{Guid.NewGuid():N}@rbac.local";

        await _client.PostAsJsonAsync("/auth/register", new
        {
            name = "Target User",
            email = userEmail,
            password = "User@123"
        });

        var createRoleResponse = await SendAuthorizedAsync(adminToken, HttpMethod.Post, "/roles", new { name = roleName });
        var createdRole = await createRoleResponse.Content.ReadFromJsonAsync<RoleResponse>();
        Assert.NotNull(createdRole);

        var usersResponse = await SendAuthorizedAsync(adminToken, HttpMethod.Get, "/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListResponse>>();
        Assert.NotNull(users);

        var targetUser = users!.FirstOrDefault(user => user.Email == userEmail);
        Assert.NotNull(targetUser);

        var assignResponse = await SendAuthorizedAsync(
            adminToken,
            HttpMethod.Post,
            $"/users/{targetUser!.Id}/roles",
            new { roleId = createdRole!.Id });

        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
    }

    [Fact]
    public async Task ListPermissions_AsAdmin_ReturnsData()
    {
        var adminToken = await LoginAsAdminAsync();

        var response = await SendAuthorizedAsync(adminToken, HttpMethod.Get, "/permissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<PermissionResponse>>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!);
    }

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@rbac.local",
            password = "Admin@123"
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return payload!.Token;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(string token, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private sealed record AuthResponse(string Token, string Email, string Name, string[] Roles);

    private sealed class ProblemDetailsResponse
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public string? TraceId { get; set; }
        public string? CorrelationId { get; set; }
    }

    private sealed class RoleResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class UserListResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    private sealed class PermissionResponse
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
    }
}
