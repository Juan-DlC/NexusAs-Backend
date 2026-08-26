using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configuration
{
    public class BusinessPartnerConfiguration : IEntityTypeConfiguration<BusinessPartner>
    {
        public void Configure(EntityTypeBuilder<BusinessPartner> builder)
        {
            builder.ToTable("BusinessPartners");

            builder.HasKey(bp => bp.Id);

            builder.Property(bp => bp.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(bp => bp.DocumentNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(bp => bp.Email)
                .HasMaxLength(100);

            builder.Property(bp => bp.Phone)
                .HasMaxLength(20);

            builder.Property(bp => bp.Address)
                .HasMaxLength(200);

            builder.Property(bp => bp.CommissionPercent)
                .HasPrecision(5, 2);

            builder.Property(bp => bp.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Índice único para DocumentNumber activos
            builder.HasIndex(bp => new { bp.DocumentNumber, bp.IsActive })
                .HasDatabaseName("IX_BusinessPartners_DocumentNumber_IsActive");

            // Relación uno a muchos con Products
            builder.HasMany(bp => bp.Products)
                .WithOne(p => p.BusinessPartner)
                .HasForeignKey(p => p.BusinessPartnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación uno a muchos con BusinessPartnerLiquidations
            builder.HasMany(bp => bp.Liquidations)
                .WithOne(l => l.BusinessPartner)
                .HasForeignKey(l => l.BusinessPartnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
