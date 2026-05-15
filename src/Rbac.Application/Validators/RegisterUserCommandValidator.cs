using FluentValidation;
using Rbac.Application.Commands.Auth;

namespace Rbac.Application.Validators;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(12).WithMessage("Senha deve ter no mínimo 12 caracteres.")
            .Matches("[A-Z]").WithMessage("Senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("Senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("Senha deve conter ao menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Senha deve conter ao menos um símbolo.");
    }
}
