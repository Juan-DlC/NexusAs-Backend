using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configuration
{
    public class BusinessPartnerLiquidationConfiguration : IEntityTypeConfiguration<BusinessPartnerLiquidation>
    {
        public void Configure(EntityTypeBuilder<BusinessPartnerLiquidation> builder)
        {
            builder.ToTable("BusinessPartnerLiquidations");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.LiquidationNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(l => l.TotalAmount)
                .HasPrecision(18, 2);

            builder.Property(l => l.Notes)
                .HasMaxLength(500);

            builder.Property(l => l.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Índice único para LiquidationNumber
            builder.HasIndex(l => l.LiquidationNumber)
                .IsUnique()
                .HasDatabaseName("IX_BusinessPartnerLiquidations_LiquidationNumber");

            // Índice para búsquedas por BusinessPartnerId
            builder.HasIndex(l => l.BusinessPartnerId)
                .HasDatabaseName("IX_BusinessPartnerLiquidations_BusinessPartnerId");

            // Índice para búsquedas por fecha
            builder.HasIndex(l => l.LiquidationDate)
                .HasDatabaseName("IX_BusinessPartnerLiquidations_LiquidationDate");

            // Relación con BusinessPartner
            builder.HasOne(l => l.BusinessPartner)
                .WithMany(bp => bp.Liquidations)
                .HasForeignKey(l => l.BusinessPartnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con User (quien procesó la liquidación)
            builder.HasOne(l => l.ProcessedByUser)
                .WithMany()
                .HasForeignKey(l => l.ProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación uno a muchos con BusinessPartnerLiquidationDetails
            builder.HasMany(l => l.Details)
                .WithOne(d => d.Liquidation)
                .HasForeignKey(d => d.LiquidationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
