using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configuration
{
    public class BusinessPartnerLiquidationDetailConfiguration : IEntityTypeConfiguration<BusinessPartnerLiquidationDetail>
    {
        public void Configure(EntityTypeBuilder<BusinessPartnerLiquidationDetail> builder)
        {
            builder.ToTable("BusinessPartnerLiquidationDetails");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.CostPrice)
                .HasPrecision(18, 2);

            builder.Property(d => d.SalePrice)
                .HasPrecision(18, 2);

            builder.Property(d => d.GrossProfit)
                .HasPrecision(18, 2);

            builder.Property(d => d.PartnerCommissionPercent)
                .HasPrecision(5, 2);

            builder.Property(d => d.BusinessPartnerCommissionPercent)
                .HasPrecision(5, 2);

            builder.Property(d => d.PartnerCommissionAmount)
                .HasPrecision(18, 2);

            builder.Property(d => d.RemainingProfit)
                .HasPrecision(18, 2);

            builder.Property(d => d.BusinessPartnerAmount)
                .HasPrecision(18, 2);

            builder.Property(d => d.AsAmount)
                .HasPrecision(18, 2);

            builder.Property(d => d.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Índice para búsquedas por LiquidationId
            builder.HasIndex(d => d.LiquidationId)
                .HasDatabaseName("IX_BusinessPartnerLiquidationDetails_LiquidationId");

            // Relación con Liquidation
            builder.HasOne(d => d.Liquidation)
                .WithMany(l => l.Details)
                .HasForeignKey(d => d.LiquidationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con Sale
            builder.HasOne(d => d.Sale)
                .WithMany()
                .HasForeignKey(d => d.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con Product
            builder.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
