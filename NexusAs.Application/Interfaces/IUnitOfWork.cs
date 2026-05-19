using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Category> Categories { get; }
        IRepository<Product> Products { get; }
        IRepository<Customer> Customers { get; }
        IRepository<User> Users { get; }
        IRepository<Sale> Sales { get; }
        IRepository<SaleDetail> SaleDetails { get; }
        IRepository<StockMovement> StockMovements { get; }
        ICreditRepository Credits { get; }
        IRepository<PartnerConfig> PartnerConfigs { get; }
        IRepository<PartnerProductPrice> PartnerProductPrices { get; }
        IRepository<PartnerSale> PartnerSales { get; }
        IRepository<PartnerLiquidation> PartnerLiquidations { get; }
        Task<int> SaveChangesAsync();
    }
}