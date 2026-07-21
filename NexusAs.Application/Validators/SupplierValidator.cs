using FluentValidation;
using NexusAs.Application.DTOs.Suppliers;

namespace NexusAs.Application.Validators
{
    public class CreateSupplierValidator : AbstractValidator<CreateSupplierDto>
    {
        public CreateSupplierValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres.")
                .When(x => x.Phone != null);

            RuleFor(x => x.Email)
                .MaximumLength(100).WithMessage("El email no puede superar 100 caracteres.")
                .EmailAddress().WithMessage("El email no es válido.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Las notas no pueden superar 1000 caracteres.")
                .When(x => x.Notes != null);
        }
    }

    public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierDto>
    {
        public UpdateSupplierValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres.")
                .When(x => x.Phone != null);

            RuleFor(x => x.Email)
                .MaximumLength(100).WithMessage("El email no puede superar 100 caracteres.")
                .EmailAddress().WithMessage("El email no es válido.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Las notas no pueden superar 1000 caracteres.")
                .When(x => x.Notes != null);
        }
    }
}
