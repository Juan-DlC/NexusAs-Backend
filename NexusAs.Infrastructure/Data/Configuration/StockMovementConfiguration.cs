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
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.ToTable("StockMovements");
            builder.HasKey(sm => sm.Id);
            builder.Property(sm => sm.Date).IsRequired();
            builder.Property(sm => sm.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(sm => sm.Quantity).IsRequired();
            builder.Property(sm => sm.Reason).HasMaxLength(500);
            builder.Property(sm => sm.CreatedAt).IsRequired();
            builder.HasOne(sm => sm.Product)
                   .WithMany(p => p.StockMovements)
                   .HasForeignKey(sm => sm.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(sm => sm.User)
                   .WithMany(u => u.StockMovements)
                   .HasForeignKey(sm => sm.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
