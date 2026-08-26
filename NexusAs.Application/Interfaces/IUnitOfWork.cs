using NexusAs.Domain.Entities;

namespace NexusAs.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Category> Categories { get; }
        IProductRepository Products { get; }
        IRepository<Supplier> Suppliers { get; }
        IRepository<Customer> Customers { get; }
        IRepository<PaymentMethodEntity> PaymentMethods { get; }
        IUserRepository Users { get; }
        ISaleRepository Sales { get; }
        IRepository<SaleDetail> SaleDetails { get; }
        IStockRepository StockMovements { get; }
        ICreditRepository Credits { get; }
        IRepository<PartnerConfig> PartnerConfigs { get; }
        IRepository<PartnerProductPrice> PartnerProductPrices { get; }
        IRepository<PartnerSale> PartnerSales { get; }
        IRepository<PartnerLiquidation> PartnerLiquidations { get; }
        Task<int> SaveChangesAsync();
        IRepository<CreditInstallment> CreditInstallments { get; }
        IRepository<CreditPayment> CreditPayments { get; }
        IRepository<Return> Returns { get; }
        IRepository<ReturnDetail> ReturnDetails { get; }
        IBusinessPartnerRepository BusinessPartners { get; }
        IRepository<BusinessPartnerLiquidation> BusinessPartnerLiquidations { get; }
        IRepository<BusinessPartnerLiquidationDetail> BusinessPartnerLiquidationDetails { get; }
    }
}