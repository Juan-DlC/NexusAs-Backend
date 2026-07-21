using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethodEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentMethodEntity> builder)
        {
            builder.ToTable("PaymentMethods");
            builder.HasKey(pm => pm.Id);
            builder.Property(pm => pm.Name).IsRequired().HasMaxLength(100);
            builder.Property(pm => pm.Code).IsRequired().HasMaxLength(50);
            builder.Property(pm => pm.Description).HasMaxLength(500);
            builder.Property(pm => pm.IsActive).HasDefaultValue(true);
            builder.Property(pm => pm.CreatedAt).IsRequired();

            // Índice único en Code
            builder.HasIndex(pm => pm.Code).IsUnique();

            // Relación con Sales
            builder.HasMany(pm => pm.Sales)
                .WithOne(s => s.PaymentMethodEntity)
                .HasForeignKey(s => s.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data inicial
            builder.HasData(
                new PaymentMethodEntity
                {
                    Id = 1,
                    Name = "Contado",
                    Code = "CASH",
                    Description = "Pago en efectivo",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new PaymentMethodEntity
                {
                    Id = 2,
                    Name = "Crédito",
                    Code = "CREDIT",
                    Description = "Pago a crédito",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new PaymentMethodEntity
                {
                    Id = 3,
                    Name = "Addi",
                    Code = "ADDI",
                    Description = "Pago mediante plataforma Addi",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new PaymentMethodEntity
                {
                    Id = 4,
                    Name = "Sistecredito",
                    Code = "SISTECREDITO",
                    Description = "Pago mediante Sistecredito",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new PaymentMethodEntity
                {
                    Id = 5,
                    Name = "Tarjeta",
                    Code = "CARD",
                    Description = "Pago con tarjeta de crédito o débito",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            );
        }
    }
}
