using Challenge.Core.Dtos;

namespace Challenge.Core.Interfaces;

public interface ITransactionService
{
    /// <summary>
    /// Creates a transaction for an existing user. Returns <c>null</c> when the
    /// referenced user does not exist.
    /// </summary>
    Task<TransactionDto?> AddAsync(CreateTransactionDto dto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDto>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserTotalDto>> GetTotalPerUserAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionTypeTotalDto>> GetTotalPerTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns transactions whose amount is greater than or equal to <paramref name="threshold"/>,
    /// ordered by amount descending.
    /// </summary>
    Task<IReadOnlyList<TransactionDto>> GetHighVolumeAsync(decimal threshold, CancellationToken cancellationToken = default);
}
