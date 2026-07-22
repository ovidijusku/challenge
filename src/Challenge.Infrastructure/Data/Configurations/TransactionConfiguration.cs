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

        // Covering index for the high-volume query (WHERE Amount > threshold).
        // INCLUDE-ing the remaining columns lets SQL Server satisfy the query
        // entirely from the index and skip key lookups back to the table.
        builder.HasIndex(t => t.Amount)
            .IncludeProperties(t => new { t.UserId, t.TransactionType, t.CreatedAt });

        // Covering index for fetching a single user's transactions (WHERE UserId = @id).
        builder.HasIndex(t => t.UserId)
            .IncludeProperties(t => new { t.Amount, t.TransactionType, t.CreatedAt });
    }
}
