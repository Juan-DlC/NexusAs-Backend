using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class ReturnDetailConfiguration : IEntityTypeConfiguration<ReturnDetail>
    {
        public void Configure(EntityTypeBuilder<ReturnDetail> builder)
        {
            builder.ToTable("ReturnDetails");
            builder.HasKey(rd => rd.Id);
            builder.Property(rd => rd.Quantity).IsRequired();
            builder.Property(rd => rd.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(rd => rd.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(rd => rd.IsActive).HasDefaultValue(true);
            builder.Property(rd => rd.CreatedAt).IsRequired();

            // Relación con Return
            builder.HasOne(rd => rd.Return)
                .WithMany(r => r.ReturnDetails)
                .HasForeignKey(rd => rd.ReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación con Product
            builder.HasOne(rd => rd.Product)
                .WithMany(p => p.ReturnDetails)
                .HasForeignKey(rd => rd.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
