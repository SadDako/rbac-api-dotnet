using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Rbac.Infrastructure.Authorization;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        if (policyName.Contains('.') || policyName.StartsWith("Permission", StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase)
                ? policyName.Substring("Permission:".Length)
                : policyName;

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
