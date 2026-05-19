using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class PartnerLiquidationConfiguration
        : IEntityTypeConfiguration<PartnerLiquidation>
    {
        public void Configure(EntityTypeBuilder<PartnerLiquidation> builder)
        {
            builder.ToTable("PartnerLiquidations");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.Type)
                .HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.Notes).HasMaxLength(500);
            builder.Property(p => p.Date).IsRequired();
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.HasOne(p => p.PartnerConfig)
                .WithMany(pc => pc.Liquidations)
                .HasForeignKey(p => p.PartnerConfigId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}