using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class PartnerConfigConfiguration : IEntityTypeConfiguration<PartnerConfig>
    {
        public void Configure(EntityTypeBuilder<PartnerConfig> builder)
        {
            builder.ToTable("PartnerConfigs");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.CommissionPercent)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(50);
            builder.Property(p => p.Notes).HasMaxLength(500);
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<PartnerConfig>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}