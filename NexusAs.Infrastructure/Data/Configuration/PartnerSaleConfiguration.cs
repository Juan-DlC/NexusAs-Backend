using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class PartnerSaleConfiguration : IEntityTypeConfiguration<PartnerSale>
    {
        public void Configure(EntityTypeBuilder<PartnerSale> builder)
        {
            builder.ToTable("PartnerSales");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            builder.Property(p => p.PartnerPrice).HasColumnType("decimal(18,2)");
            builder.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
            builder.Property(p => p.CommissionPercent).HasColumnType("decimal(5,2)");
            builder.Property(p => p.PartnerEarning).HasColumnType("decimal(18,2)");
            builder.Property(p => p.AsEarning).HasColumnType("decimal(18,2)");
            builder.Property(p => p.Date).IsRequired();
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.HasOne(p => p.PartnerConfig)
                .WithMany(pc => pc.PartnerSales)
                .HasForeignKey(p => p.PartnerConfigId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.Sale)
                .WithMany()
                .HasForeignKey(p => p.SaleId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}