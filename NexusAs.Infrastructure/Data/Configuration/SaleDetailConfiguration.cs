using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
    {
        public void Configure(EntityTypeBuilder<SaleDetail> builder)
        {
            builder.ToTable("SaleDetails");
            builder.HasKey(sd => sd.Id);
            builder.Property(sd => sd.Quantity).IsRequired();
            builder.Property(sd => sd.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(sd => sd.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(sd => sd.CreatedAt).IsRequired();
            builder.HasOne(sd => sd.Sale)
                   .WithMany(s => s.SaleDetails)
                   .HasForeignKey(sd => sd.SaleId)
                   .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(sd => sd.Product)
                   .WithMany(p => p.SaleDetails)
                   .HasForeignKey(sd => sd.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
