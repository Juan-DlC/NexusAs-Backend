using FluentValidation;
using NexusAs.Application.DTOs.Sales;

namespace NexusAs.Application.Validators
{
    public class CreateSaleValidator : AbstractValidator<CreateSaleDto>
    {
        public CreateSaleValidator()
        {
            // TAREA 2: Validación explícita de PaymentMethodId en FluentValidation
            RuleFor(x => x.PaymentMethodId)
                .GreaterThan(0).WithMessage("Debe seleccionar un método de pago válido.");

            RuleFor(x => x.Discount)
                .GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo.");

            RuleFor(x => x.Details)
                .NotEmpty().WithMessage("La venta debe tener al menos un producto.");

            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.ProductId)
                    .GreaterThan(0).WithMessage("El producto no es válido.");
                detail.RuleFor(d => d.Quantity)
                    .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
                detail.RuleFor(d => d.UnitPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
            });
        }
    }
}