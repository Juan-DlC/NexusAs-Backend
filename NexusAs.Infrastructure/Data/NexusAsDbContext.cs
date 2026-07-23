using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data
{
    public class NexusAsDbContext : DbContext
    {
        public NexusAsDbContext(DbContextOptions<NexusAsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PaymentMethodEntity> PaymentMethods { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Credit> Credits { get; set; }
        public DbSet<PartnerConfig> PartnerConfigs { get; set; }
        public DbSet<PartnerProductPrice> PartnerProductPrices { get; set; }
        public DbSet<PartnerSale> PartnerSales { get; set; }
        public DbSet<PartnerLiquidation> PartnerLiquidations { get; set; }
        public DbSet<CreditPayment> CreditPayments { get; set; }
        public DbSet<CreditInstallment> CreditInstallments { get; set; }
        public DbSet<Return> Returns { get; set; }
        public DbSet<ReturnDetail> ReturnDetails { get; set; }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.Now;
                        entry.Entity.IsActive = true;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.Now;
                        break;
                }
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(
                System.Reflection.Assembly.GetExecutingAssembly());
        }

    }
}