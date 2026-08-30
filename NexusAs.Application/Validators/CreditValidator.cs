using FluentValidation;
using NexusAs.Application.DTOs.Credits;

namespace NexusAs.Application.Validators
{
    public class PaymentDtoValidator : AbstractValidator<PaymentDto>
    {
        public PaymentDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("El monto del pago debe ser mayor a 0.");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Las notas no pueden superar 500 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}
