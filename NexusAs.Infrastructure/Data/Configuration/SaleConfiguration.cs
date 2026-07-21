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
    public class SaleConfiguration : IEntityTypeConfiguration<Sale>
    {
        public void Configure(EntityTypeBuilder<Sale> builder)
        {
            builder.ToTable("Sales");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(20);
            builder.HasIndex(s => s.SaleNumber).IsUnique();
            builder.Property(s => s.Date).IsRequired();
            builder.Property(s => s.Subtotal).HasColumnType("decimal(18,2)");
            builder.Property(s => s.Discount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            builder.Property(s => s.Total).HasColumnType("decimal(18,2)");
            builder.Property(s => s.Notes).HasMaxLength(500);
            builder.Property(s => s.CreatedAt).IsRequired();
            builder.HasOne(s => s.Customer)
                   .WithMany(c => c.Sales)
                   .HasForeignKey(s => s.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.User)
                   .WithMany(u => u.Sales)
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
