using Challenge.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Challenge.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.TransactionType)
            .HasConversion<int>();

        // Supports the high-volume query (WHERE Amount >= threshold, ORDER BY Amount).
        builder.HasIndex(t => t.Amount);

        // Supports fetching a single user's transactions (WHERE UserId = @id).
        builder.HasIndex(t => t.UserId);
    }
}
