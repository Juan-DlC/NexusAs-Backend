using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class CreditInstallmentConfiguration : IEntityTypeConfiguration<CreditInstallment>
    {
        public void Configure(EntityTypeBuilder<CreditInstallment> builder)
        {
            builder.ToTable("CreditInstallments");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Amount).HasColumnType("decimal(18,2)");
            builder.Property(i => i.Number).IsRequired();
            builder.Property(i => i.CreatedAt).IsRequired();
            builder.HasOne(i => i.Credit)
                .WithMany(c => c.Installments)
                .HasForeignKey(i => i.CreditId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}