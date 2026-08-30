using FluentValidation;
using NexusAs.Application.DTOs.BusinessPartners;

namespace NexusAs.Application.Validators
{
    public class CreateBusinessPartnerValidator : AbstractValidator<CreateBusinessPartnerDto>
    {
        public CreateBusinessPartnerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.DocumentNumber)
                .NotEmpty().WithMessage("El número de documento es obligatorio.")
                .MaximumLength(50).WithMessage("El documento no puede superar 50 caracteres.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El email no tiene un formato válido.")
                .MaximumLength(100).WithMessage("El email no puede superar 100 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.CommissionPercent)
                .GreaterThanOrEqualTo(0).WithMessage("El porcentaje de comisión no puede ser negativo.")
                .LessThanOrEqualTo(100).WithMessage("El porcentaje de comisión no puede ser mayor a 100.");
        }
    }

    public class UpdateBusinessPartnerValidator : AbstractValidator<UpdateBusinessPartnerDto>
    {
        public UpdateBusinessPartnerValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.DocumentNumber)
                .NotEmpty().WithMessage("El número de documento es obligatorio.")
                .MaximumLength(50).WithMessage("El documento no puede superar 50 caracteres.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El email no tiene un formato válido.")
                .MaximumLength(100).WithMessage("El email no puede superar 100 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.CommissionPercent)
                .GreaterThanOrEqualTo(0).WithMessage("El porcentaje de comisión no puede ser negativo.")
                .LessThanOrEqualTo(100).WithMessage("El porcentaje de comisión no puede ser mayor a 100.");
        }
    }
}
