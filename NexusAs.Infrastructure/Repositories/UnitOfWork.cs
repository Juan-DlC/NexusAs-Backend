using NexusAs.Application.Interfaces;
using NexusAs.Domain.Entities;
using NexusAs.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexusAs.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly NexusAsDbContext _context;

        private IRepository<Category>? _categories;
        private IProductRepository? _products;
        private IRepository<Supplier>? _suppliers;
        private IRepository<Customer>? _customers;
        private IRepository<PaymentMethodEntity>? _paymentMethods;
        private IUserRepository? _users;
        private ISaleRepository? _sales;
        private IRepository<SaleDetail>? _saleDetails;
        private IStockRepository? _stockMovements;
        private ICreditRepository? _credits;
        private IRepository<PartnerConfig>? _partnerConfigs;
        private IRepository<PartnerProductPrice>? _partnerProductPrices;
        private IRepository<PartnerSale>? _partnerSales;
        private IRepository<PartnerLiquidation>? _partnerLiquidations;

        public UnitOfWork(NexusAsDbContext context)
        {
            _context = context;
        }

        public IRepository<Category> Categories =>
            _categories ??= new BaseRepository<Category>(_context);
        public IProductRepository Products =>
              _products ??= new ProductRepository(_context);
        public IRepository<Supplier> Suppliers =>
            _suppliers ??= new BaseRepository<Supplier>(_context);
        public IRepository<Customer> Customers =>
            _customers ??= new BaseRepository<Customer>(_context);
        public IRepository<PaymentMethodEntity> PaymentMethods =>
            _paymentMethods ??= new BaseRepository<PaymentMethodEntity>(_context);
        public IUserRepository Users =>
            _users ??= new UserRepository(_context);
        public ISaleRepository Sales =>
            _sales ??= new SaleRepository(_context);
        public IRepository<SaleDetail> SaleDetails =>
            _saleDetails ??= new BaseRepository<SaleDetail>(_context);
        public IStockRepository StockMovements =>
             _stockMovements ??= new StockRepository(_context);
        public ICreditRepository Credits =>
            _credits ??= new CreditRepository(_context);
        public IRepository<PartnerConfig> PartnerConfigs =>
            _partnerConfigs ??= new BaseRepository<PartnerConfig>(_context);
        public IRepository<PartnerProductPrice> PartnerProductPrices =>
            _partnerProductPrices ??= new BaseRepository<PartnerProductPrice>(_context);
        public IRepository<PartnerSale> PartnerSales =>
            _partnerSales ??= new BaseRepository<PartnerSale>(_context);
        public IRepository<PartnerLiquidation> PartnerLiquidations =>
            _partnerLiquidations ??= new BaseRepository<PartnerLiquidation>(_context);
        private IRepository<CreditInstallment>? _creditInstallments;
        private IRepository<CreditPayment>? _creditPayments;

        public IRepository<CreditInstallment> CreditInstallments =>
            _creditInstallments ??= new BaseRepository<CreditInstallment>(_context);

        public IRepository<CreditPayment> CreditPayments =>
            _creditPayments ??= new BaseRepository<CreditPayment>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}