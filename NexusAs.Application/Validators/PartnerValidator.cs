using FluentValidation;
using NexusAs.Application.DTOs.Partners;

namespace NexusAs.Application.Validators
{
    public class CreatePartnerConfigValidator : AbstractValidator<CreatePartnerConfigDto>
    {
        public CreatePartnerConfigValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Debe seleccionar un usuario válido.");

            RuleFor(x => x.CommissionPercent)
                .GreaterThanOrEqualTo(0).WithMessage("El porcentaje de comisión no puede ser negativo.")
                .LessThanOrEqualTo(100).WithMessage("El porcentaje de comisión no puede ser mayor a 100.");
        }
    }

    public class UpdatePartnerCommissionValidator : AbstractValidator<UpdatePartnerCommissionDto>
    {
        public UpdatePartnerCommissionValidator()
        {
            RuleFor(x => x.CommissionPercent)
                .GreaterThanOrEqualTo(0).WithMessage("El porcentaje de comisión no puede ser negativo.")
                .LessThanOrEqualTo(100).WithMessage("El porcentaje de comisión no puede ser mayor a 100.");

            RuleFor(x => x.AllianceCommissionPercent)
                .GreaterThanOrEqualTo(0).WithMessage("El porcentaje de comisión en alianza no puede ser negativo.")
                .LessThanOrEqualTo(100).WithMessage("El porcentaje de comisión en alianza no puede ser mayor a 100.");
        }
    }
}
