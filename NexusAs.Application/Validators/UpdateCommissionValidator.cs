using FluentValidation;
using NexusAs.Application.DTOs.BusinessPartners;

namespace NexusAs.Application.Validators
{
    public class UpdateCommissionValidator : AbstractValidator<UpdateCommissionDto>
    {
        public UpdateCommissionValidator()
        {
            RuleFor(x => x.CommissionPercent)
                .GreaterThanOrEqualTo(0).WithMessage("El porcentaje de comisión debe ser mayor o igual a 0.")
                .LessThanOrEqualTo(100).WithMessage("El porcentaje de comisión no puede superar el 100%.");
        }
    }
}
