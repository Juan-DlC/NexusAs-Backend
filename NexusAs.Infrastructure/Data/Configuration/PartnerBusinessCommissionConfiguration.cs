using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configuration
{
    public class PartnerBusinessCommissionConfiguration : IEntityTypeConfiguration<PartnerBusinessCommission>
    {
        public void Configure(EntityTypeBuilder<PartnerBusinessCommission> builder)
        {
            builder.HasKey(pbc => pbc.Id);
            
            builder.Property(pbc => pbc.CommissionPercent)
                .HasColumnType("decimal(5,2)");

            builder.HasIndex(pbc => new { pbc.PartnerConfigId, pbc.BusinessPartnerId })
                .IsUnique()
                .HasDatabaseName("IX_PartnerBusinessCommissions_Unique");

            builder.HasOne(pbc => pbc.PartnerConfig)
                .WithMany(pc => pc.BusinessCommissions)
                .HasForeignKey(pbc => pbc.PartnerConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pbc => pbc.BusinessPartner)
                .WithMany(bp => bp.PartnerCommissions)
                .HasForeignKey(pbc => pbc.BusinessPartnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
