using FluentValidation;
using NexusAs.Application.DTOs.Products;

namespace NexusAs.Application.Validators
{
    public class CreateProductValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(50).WithMessage("El código no puede superar 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.Cost)
                .GreaterThan(0).WithMessage("El costo debe ser mayor a 0.");

            RuleFor(x => x.SalePrice)
                .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0.")
                .GreaterThanOrEqualTo(x => x.Cost)
                .WithMessage("El precio de venta debe ser mayor o igual al costo.");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");

            RuleFor(x => x.MinStock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida.");
        }
    }

    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(50).WithMessage("El código no puede superar 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

            RuleFor(x => x.Cost)
                .GreaterThan(0).WithMessage("El costo debe ser mayor a 0.");

            RuleFor(x => x.SalePrice)
                .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a 0.")
                .GreaterThanOrEqualTo(x => x.Cost)
                .WithMessage("El precio de venta debe ser mayor o igual al costo.");

            RuleFor(x => x.MinStock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida.");
        }
    }
}