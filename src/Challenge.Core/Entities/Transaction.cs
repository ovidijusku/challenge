using Challenge.Core.Enums;

namespace Challenge.Core.Entities;

public class Transaction
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public TransactionType TransactionType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
