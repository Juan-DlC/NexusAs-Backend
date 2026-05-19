using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class PartnerProductPriceConfiguration
        : IEntityTypeConfiguration<PartnerProductPrice>
    {
        public void Configure(EntityTypeBuilder<PartnerProductPrice> builder)
        {
            builder.ToTable("PartnerProductPrices");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.CustomCommission)
                .HasColumnType("decimal(5,2)");
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.HasOne(p => p.PartnerConfig)
                .WithMany(pc => pc.ProductPrices)
                .HasForeignKey(p => p.PartnerConfigId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}