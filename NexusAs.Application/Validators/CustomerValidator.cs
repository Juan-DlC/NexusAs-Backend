using FluentValidation;
using NexusAs.Application.DTOs.Customers;

namespace NexusAs.Application.Validators
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.Document)
                .MaximumLength(20).WithMessage("El documento no puede superar 20 caracteres.")
                .When(x => x.Document != null);

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres.")
                .When(x => x.Phone != null);

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El email no tiene un formato válido.")
                .When(x => x.Email != null);
        }
    }

    public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
    {
        public UpdateCustomerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El email no tiene un formato válido.")
                .When(x => x.Email != null);
        }
    }
}