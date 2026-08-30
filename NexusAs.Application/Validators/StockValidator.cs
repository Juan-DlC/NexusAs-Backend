using FluentValidation;
using NexusAs.Application.DTOs.Stock;

namespace NexusAs.Application.Validators
{
    public class StockEntryValidator : AbstractValidator<StockEntryDto>
    {
        public StockEntryValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Debe seleccionar un producto válido.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("La razón no puede superar 500 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }

    public class StockAdjustmentValidator : AbstractValidator<StockAdjustmentDto>
    {
        public StockAdjustmentValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("Debe seleccionar un producto válido.");

            RuleFor(x => x.NewStock)
                .GreaterThanOrEqualTo(0).WithMessage("El nuevo stock no puede ser negativo.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Debe indicar la razón del ajuste.")
                .MaximumLength(500).WithMessage("La razón no puede superar 500 caracteres.");
        }
    }
}
