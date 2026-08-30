using FluentValidation;
using NexusAs.Application.DTOs.Users;

namespace NexusAs.Application.Validators
{
    public class CreateUserValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                .MinimumLength(4).WithMessage("El nombre de usuario debe tener al menos 4 caracteres.")
                .MaximumLength(50).WithMessage("El nombre de usuario no puede superar 50 caracteres.")
                .Matches("^[a-zA-Z0-9_.-]+$")
                .WithMessage("El nombre de usuario solo puede contener letras, números, guiones, puntos y guiones bajos.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
                .MaximumLength(100).WithMessage("La contraseña no puede superar 100 caracteres.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("El nombre completo es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre completo no puede superar 200 caracteres.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("El rol es obligatorio.")
                .Must(role => role == "Admin" || role == "Seller" || role == "Partner")
                .WithMessage("El rol debe ser Admin, Seller o Partner.");
        }
    }

    public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres.")
                .MaximumLength(100).WithMessage("La nueva contraseña no puede superar 100 caracteres.")
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("La nueva contraseña debe ser diferente a la actual.");
        }
    }
}
