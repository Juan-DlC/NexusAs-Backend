using FluentValidation;
using NexusAs.Application.DTOs.Returns;

namespace NexusAs.Application.Validators
{
    public class CreateReturnValidator : AbstractValidator<CreateReturnDto>
    {
        public CreateReturnValidator()
        {
            RuleFor(x => x.SaleId)
                .GreaterThan(0).WithMessage("Debe seleccionar una venta.");

            RuleFor(x => x.Details)
                .NotEmpty().WithMessage("Debe agregar al menos un producto a devolver.");

            RuleForEach(x => x.Details).ChildRules(detail =>
            {
                detail.RuleFor(d => d.ProductId)
                    .GreaterThan(0).WithMessage("ID de producto inválido.");

                detail.RuleFor(d => d.Quantity)
                    .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
            });

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Las notas no pueden superar 1000 caracteres.")
                .When(x => x.Notes != null);
        }
    }
}
