using AutoMapper;
using Challenge.Core.Dtos;
using Challenge.Core.Entities;
using Challenge.Core.Interfaces;

namespace Challenge.Core.Services;

public class TransactionService(IRepository<Transaction> transactions, IRepository<User> users, IMapper mapper) : ITransactionService
{
    public async Task<TransactionDto?> AddAsync(CreateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(dto.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var transaction = mapper.Map<Transaction>(dto);
        transaction.CreatedAt = DateTime.UtcNow;
        await transactions.AddAsync(transaction, cancellationToken);
        await transactions.SaveChangesAsync(cancellationToken);
        return mapper.Map<TransactionDto>(transaction);
    }

    public async Task<IReadOnlyList<TransactionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await transactions.GetAllAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<TransactionDto>>(result);
    }

    public async Task<IReadOnlyList<TransactionDto>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = await transactions.FindAsync(t => t.UserId == userId, cancellationToken);
        return mapper.Map<IReadOnlyList<TransactionDto>>(result);
    }

    public async Task<IReadOnlyList<UserTotalDto>> GetTotalPerUserAsync(CancellationToken cancellationToken = default)
    {
        var allTransactions = await transactions.GetAllAsync(cancellationToken);
        var allUsers = await users.GetAllAsync(cancellationToken);

        var totalsByUser = allTransactions
            .GroupBy(t => t.UserId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        // Drive off the users list so users with no transactions are reported with a total of 0.
        return allUsers
            .Select(u => new UserTotalDto
            {
                UserId = u.Id,
                TotalAmount = totalsByUser.GetValueOrDefault(u.Id)
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TransactionTypeTotalDto>> GetTotalPerTypeAsync(CancellationToken cancellationToken = default)
    {
        var result = await transactions.GetAllAsync(cancellationToken);

        return result
            .GroupBy(t => t.TransactionType)
            .Select(g => new TransactionTypeTotalDto
            {
                TransactionType = g.Key,
                TotalAmount = g.Sum(t => t.Amount)
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TransactionDto>> GetHighVolumeAsync(decimal threshold, CancellationToken cancellationToken = default)
    {
        var result = await transactions.FindAsync(t => t.Amount >= threshold, cancellationToken);

        var ordered = result
            .OrderByDescending(t => t.Amount)
            .ToList();

        return mapper.Map<IReadOnlyList<TransactionDto>>(ordered);
    }
}
