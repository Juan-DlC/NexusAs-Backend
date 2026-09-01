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
        public DbSet<BusinessPartner> BusinessPartners { get; set; }
        public DbSet<BusinessPartnerLiquidation> BusinessPartnerLiquidations { get; set; }
        public DbSet<BusinessPartnerLiquidationDetail> BusinessPartnerLiquidationDetails { get; set; }

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

            // TAREA 5: ÍNDICES DE PERFORMANCE
            // Índices para Sales
            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.UserId)
                .HasDatabaseName("IX_Sales_UserId");

            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.Date)
                .HasDatabaseName("IX_Sales_Date");

            modelBuilder.Entity<Sale>()
                .HasIndex(s => new { s.UserId, s.Date })
                .HasDatabaseName("IX_Sales_UserId_Date");

            // Índices para PartnerSale
            modelBuilder.Entity<PartnerSale>()
                .HasIndex(ps => ps.PartnerConfigId)
                .HasDatabaseName("IX_PartnerSales_PartnerConfigId");

            modelBuilder.Entity<PartnerSale>()
                .HasIndex(ps => new { ps.PartnerConfigId, ps.IsActive })
                .HasDatabaseName("IX_PartnerSales_PartnerConfigId_IsActive");

            // Índices para Credit
            modelBuilder.Entity<Credit>()
                .HasIndex(c => c.Status)
                .HasDatabaseName("IX_Credits_Status");

            // Índices para StockMovement
            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.ProductId)
                .HasDatabaseName("IX_StockMovements_ProductId");

            // Índices para Product
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Products_IsActive");

            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.IsActive, p.CategoryId })
                .HasDatabaseName("IX_Products_IsActive_CategoryId");

            // Índices para BusinessPartnerLiquidation
            modelBuilder.Entity<BusinessPartnerLiquidation>()
                .HasIndex(l => l.BusinessPartnerId)
                .HasDatabaseName("IX_BusinessPartnerLiquidations_BusinessPartnerId");

            modelBuilder.Entity<BusinessPartnerLiquidation>()
                .HasIndex(l => l.CreatedAt)
                .HasDatabaseName("IX_BusinessPartnerLiquidations_CreatedAt");

            // Índices para PartnerLiquidation
            modelBuilder.Entity<PartnerLiquidation>()
                .HasIndex(l => l.PartnerConfigId)
                .HasDatabaseName("IX_PartnerLiquidations_PartnerConfigId");

            // Índices para Return
            modelBuilder.Entity<Return>()
                .HasIndex(r => r.SaleId)
                .HasDatabaseName("IX_Returns_SaleId");

            // Índices para CreditPayment
            modelBuilder.Entity<CreditPayment>()
                .HasIndex(cp => cp.CreditId)
                .HasDatabaseName("IX_CreditPayments_CreditId");
        }

    }
}