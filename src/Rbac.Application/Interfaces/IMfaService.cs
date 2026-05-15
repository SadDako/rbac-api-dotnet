namespace Rbac.Application.Interfaces;

public interface IMfaService
{
    Task<(string Secret, string ProvisioningUri, string QrCodeBase64)> GenerateSetupAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> VerifyAsync(Guid userId, string code, CancellationToken cancellationToken);
    Task<IEnumerable<string>> GenerateRecoveryCodesAsync(Guid userId, int count, CancellationToken cancellationToken);
    Task<bool> DisableAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ValidateRecoveryCodeAsync(Guid userId, string code, CancellationToken cancellationToken);
}
