using System.ComponentModel.DataAnnotations;
using Challenge.Core.Enums;

namespace Challenge.Core.Dtos;

public record TransactionDto
{
    public int Id { get; init; }

    public string UserId { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public TransactionType TransactionType { get; init; }

    public DateTime CreatedAt { get; init; }
}

public record CreateTransactionDto
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; init; }

    [EnumDataType(typeof(TransactionType))]
    public TransactionType TransactionType { get; init; }
}
