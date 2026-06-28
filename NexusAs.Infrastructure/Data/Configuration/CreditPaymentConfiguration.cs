using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class CreditPaymentConfiguration : IEntityTypeConfiguration<CreditPayment>
    {
        public void Configure(EntityTypeBuilder<CreditPayment> builder)
        {
            builder.ToTable("CreditPayments");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.Date).IsRequired();
            builder.Property(p => p.Notes).HasMaxLength(300);
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.HasOne(p => p.Credit)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CreditId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}