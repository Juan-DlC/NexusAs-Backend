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
    public class CreditConfiguration : IEntityTypeConfiguration<Credit>
    {
        public void Configure(EntityTypeBuilder<Credit> builder)
        {
            builder.ToTable("Credits");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(c => c.PaidAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            builder.Property(c => c.PendingAmount).HasColumnType("decimal(18,2)");
            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(c => c.Notes).HasMaxLength(500);
            builder.Property(c => c.CreatedAt).IsRequired();
            builder.HasOne(c => c.Sale)
                   .WithOne(s => s.Credit)
                   .HasForeignKey<Credit>(c => c.SaleId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.Customer)
                   .WithMany(cu => cu.Credits)
                   .HasForeignKey(c => c.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}