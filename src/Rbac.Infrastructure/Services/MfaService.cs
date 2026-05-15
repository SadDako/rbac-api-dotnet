using System.Security.Cryptography;
using System.Text.Json;
using OtpNet;
using QRCoder;
using Rbac.Application.Interfaces;
using Rbac.Infrastructure.Data;

namespace Rbac.Infrastructure.Services;

public sealed class MfaService : IMfaService
{
    private readonly AppDbContext _dbContext;
    private readonly IUserRepository _userRepository;

    public MfaService(AppDbContext dbContext, IUserRepository userRepository)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
    }

    public async Task<(string Secret, string ProvisioningUri, string QrCodeBase64)> GenerateSetupAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) throw new InvalidOperationException("User not found");

        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32 = Base32Encoding.ToString(secretKey);
        var issuer = "RbacEnterprise";
        var provisioningUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email)}?secret={Uri.EscapeDataString(base32)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

        var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var png = qrCode.GetGraphic(20);
        var base64 = Convert.ToBase64String(png);

        user.MfaSecret = base32;
        await _userRepository.SaveChangesAsync(cancellationToken);

        return (base32, provisioningUri, base64);
    }

    public async Task<bool> VerifyAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.MfaSecret)) return false;

        var secret = Base32Encoding.ToBytes(user.MfaSecret);
        var totp = new Totp(secret);
        var verified = totp.VerifyTotp(code.Trim(), out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        if (verified)
        {
            user.MfaEnabled = true;
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        return verified;
    }

    public async Task<IEnumerable<string>> GenerateRecoveryCodesAsync(Guid userId, int count, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) throw new InvalidOperationException("User not found");

        var codes = new List<string>(count);
        var hashed = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(9)).Replace("=", string.Empty);
            codes.Add(code);
            hashed.Add(global::BCrypt.Net.BCrypt.HashPassword(code));
        }

        user.RecoveryCodes = JsonSerializer.Serialize(hashed);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return codes;
    }

    public async Task<bool> DisableAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return false;

        user.MfaEnabled = false;
        user.MfaSecret = null;
        user.RecoveryCodes = null;
        await _userRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ValidateRecoveryCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.RecoveryCodes)) return false;

        var stored = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodes) ?? new List<string>();
        for (var i = 0; i < stored.Count; i++)
        {
            if (!global::BCrypt.Net.BCrypt.Verify(code, stored[i]))
            {
                continue;
            }

            stored.RemoveAt(i);
            user.RecoveryCodes = JsonSerializer.Serialize(stored);
            await _userRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }
}
