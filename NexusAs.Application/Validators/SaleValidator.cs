using FluentValidation;
using NexusAs.Application.DTOs.Sales;

namespace NexusAs.Application.Validators
{
    public class CreateSaleValidator : AbstractValidator<CreateSaleDto>
    {
        public CreateSaleValidator()
        {
            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("El método de pago es obligatorio.")
                .Must(x => x == "Cash" || x == "Credit")
                .WithMessage("El método de pago debe ser 'Cash' o 'Credit'.");

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
                    .GreaterThan(0).WithMessage("El precio unitario debe ser mayor a 0.");
            });
        }
    }
}