using AutoMapper;
using NexusAs.Application.DTOs.Stock;
using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Domain.Enums;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StockService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<StockMovementDto>> GetMovementsByProductAsync(int productId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
                throw new NotFoundException(nameof(Product), productId);

            var movements = await _unitOfWork.StockMovements.GetMovementsByProductAsync(productId);
            return _mapper.Map<IEnumerable<StockMovementDto>>(movements);
        }

        public async Task<StockMovementDto> RegisterEntryAsync(StockEntryDto dto, int userId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new NotFoundException(nameof(Product), dto.ProductId);

            if (dto.Quantity <= 0)
                throw new BusinessException("La cantidad de entrada debe ser mayor a 0.");

            var stockBefore = product.Stock;
            product.Stock += dto.Quantity;
            _unitOfWork.Products.Update(product);

            var movement = new StockMovement
            {
                ProductId = dto.ProductId,
                Date = DateTime.Now,
                Type = MovementType.Entry,
                Quantity = dto.Quantity,
                StockBefore = stockBefore,
                StockAfter = product.Stock,
                Reason = dto.Reason ?? "Entrada manual de stock",
                UserId = userId
            };

            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<StockMovementDto>(movement);
        }

        public async Task<StockMovementDto> RegisterAdjustmentAsync(
            StockAdjustmentDto dto, int userId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new NotFoundException(nameof(Product), dto.ProductId);

            if (dto.NewStock < 0)
                throw new BusinessException("El stock no puede ser negativo.");

            var stockBefore = product.Stock;
            var difference = dto.NewStock - stockBefore;
            product.Stock = dto.NewStock;
            _unitOfWork.Products.Update(product);

            var movement = new StockMovement
            {
                ProductId = dto.ProductId,
                Date = DateTime.UtcNow,
                Type = MovementType.Adjustment,
                Quantity = difference,
                StockBefore = stockBefore,
                StockAfter = dto.NewStock,
                Reason = dto.Reason ?? "Ajuste manual de inventario",
                UserId = userId
            };

            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<StockMovementDto>(movement);
        }
    }
}