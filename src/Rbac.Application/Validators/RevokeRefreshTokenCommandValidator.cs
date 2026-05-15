using FluentValidation;
using Rbac.Application.Commands.Auth;

namespace Rbac.Application.Validators;

public sealed class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token é obrigatório.");

        RuleFor(x => x.DeviceFingerprint)
            .NotEmpty().WithMessage("Device fingerprint é obrigatória.");
    }
}
