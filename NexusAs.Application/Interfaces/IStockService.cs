using NexusAs.Application.DTOs.Stock;

namespace NexusAs.Application.Interfaces
{
    public interface IStockService
    {
        Task<IEnumerable<StockMovementDto>> GetMovementsByProductAsync(int productId);
        Task<StockMovementDto> RegisterEntryAsync(StockEntryDto dto, int userId);
        Task<StockMovementDto> RegisterAdjustmentAsync(StockAdjustmentDto dto, int userId);
    }
}