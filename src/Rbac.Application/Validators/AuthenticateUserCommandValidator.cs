using FluentValidation;
using Rbac.Application.Commands.Auth;

namespace Rbac.Application.Validators;

public sealed class AuthenticateUserCommandValidator : AbstractValidator<AuthenticateUserCommand>
{
    public AuthenticateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.");

        RuleFor(x => x.DeviceFingerprint)
            .NotEmpty().WithMessage("Device fingerprint é obrigatória.");
    }
}
