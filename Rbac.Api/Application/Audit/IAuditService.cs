using System.Security.Claims;

namespace Rbac.Api.Application.Audit;

public interface IAuditService
{
    Task RecordAsync(
        ClaimsPrincipal actor,
        string action,
        string target,
        string? details,
        string correlationId,
        CancellationToken cancellationToken = default);
}
