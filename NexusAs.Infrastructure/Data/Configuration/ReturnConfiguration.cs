using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusAs.Domain.Entities;

namespace NexusAs.Infrastructure.Data.Configurations
{
    public class ReturnConfiguration : IEntityTypeConfiguration<Return>
    {
        public void Configure(EntityTypeBuilder<Return> builder)
        {
            builder.ToTable("Returns");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Date).IsRequired();
            builder.Property(r => r.Notes).HasMaxLength(1000);
            builder.Property(r => r.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(r => r.IsActive).HasDefaultValue(true);
            builder.Property(r => r.CreatedAt).IsRequired();

            // Índice en Date para consultas de reportes
            builder.HasIndex(r => r.Date);

            // Relación con Sale
            builder.HasOne(r => r.Sale)
                .WithMany(s => s.Returns)
                .HasForeignKey(r => r.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con User
            builder.HasOne(r => r.User)
                .WithMany(u => u.Returns)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
