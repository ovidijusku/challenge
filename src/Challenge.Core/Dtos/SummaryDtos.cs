using Challenge.Core.Enums;

namespace Challenge.Core.Dtos;

/// <summary>Total transaction amount aggregated for a single user.</summary>
public record UserTotalDto
{
    public string UserId { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }
}

/// <summary>Total transaction amount aggregated for a single transaction type.</summary>
public record TransactionTypeTotalDto
{
    public TransactionType TransactionType { get; init; }

    public decimal TotalAmount { get; init; }
}
