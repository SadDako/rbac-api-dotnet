using Rbac.Application.Security;

namespace Rbac.Application.Interfaces;

public interface IBruteForceProtectionService
{
    Task<BruteForceAssessment> CheckAsync(string normalizedUserKey, SecurityRequestContext context, CancellationToken cancellationToken);
    Task<BruteForceAssessment> RecordFailureAsync(string normalizedUserKey, SecurityRequestContext context, CancellationToken cancellationToken);
    Task RecordSuccessAsync(string normalizedUserKey, SecurityRequestContext context, CancellationToken cancellationToken);
}
