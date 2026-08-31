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

            // Validar descuentos: solo debe usarse uno de los tres campos
            RuleFor(x => x.DiscountPercent)
                .GreaterThanOrEqualTo(0).When(x => x.DiscountPercent.HasValue)
                .WithMessage("El porcentaje de descuento no puede ser negativo.");

            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue)
                .WithMessage("El monto de descuento no puede ser negativo.");

            RuleFor(x => x.Details)
                .NotEmpty().WithMessage("La venta debe tener al menos un producto.");

            // Validar que al menos un detalle tenga ProductId válido
            RuleFor(x => x.Details)
                .Must(details => details.Any(d => d.ProductId > 0))
                .WithMessage("La venta debe tener al menos un producto válido.");

            // Validar solo los detalles con ProductId válido (ignorar detalles vacíos)
            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.ProductId)
                    .GreaterThan(0)
                    .When(d => d.ProductId > 0) // Solo validar si ProductId está presente
                    .WithMessage("El producto no es válido.");
                detail.RuleFor(d => d.Quantity)
                    .GreaterThan(0)
                    .When(d => d.ProductId > 0) // Solo validar cantidad si hay producto
                    .WithMessage("La cantidad debe ser mayor a 0.");
                detail.RuleFor(d => d.UnitPrice)
                    .GreaterThanOrEqualTo(0)
                    .When(d => d.ProductId > 0) // Solo validar precio si hay producto
                    .WithMessage("El precio unitario no puede ser negativo.");
            });
        }
    }
}