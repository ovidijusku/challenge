using System.Linq.Expressions;
using AutoMapper;
using Challenge.Core.Dtos;
using Challenge.Core.Entities;
using Challenge.Core.Enums;
using Challenge.Core.Interfaces;
using Challenge.Core.Services;
using NSubstitute;

namespace Challenge.UnitTests;

public class TransactionServiceTests
{
    private readonly IRepository<Transaction> _repository = Substitute.For<IRepository<Transaction>>();
    private readonly IMapper _mapper = TestMapperFactory.Create();

    private TransactionService CreateSut() => new(_repository, _mapper);

    private static List<Transaction> SampleData() =>
    [
        new() { Id = 1, UserId = "u1", Amount = 100m, TransactionType = TransactionType.Debit },
        new() { Id = 2, UserId = "u1", Amount = 50m, TransactionType = TransactionType.Credit },
        new() { Id = 3, UserId = "u2", Amount = 200m, TransactionType = TransactionType.Debit },
        new() { Id = 4, UserId = "u2", Amount = 25m, TransactionType = TransactionType.Credit },
    ];

    [Fact]
    public async Task GetAllAsync_MapsAllTransactions()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SampleData());

        var result = await CreateSut().GetAllAsync();

        Assert.Equal(4, result.Count);
        Assert.Contains(result, t => t.Id == 3 && t.Amount == 200m);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyMatchingUser()
    {
        _repository
            .FindAsync(Arg.Any<Expression<Func<Transaction, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(SampleData().Where(t => t.UserId == "u1").ToList());

        var result = await CreateSut().GetByUserAsync("u1");

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal("u1", t.UserId));
    }

    [Fact]
    public async Task GetTotalPerUserAsync_SumsAmountsPerUser()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SampleData());

        var result = await CreateSut().GetTotalPerUserAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(150m, result.Single(r => r.UserId == "u1").TotalAmount);
        Assert.Equal(225m, result.Single(r => r.UserId == "u2").TotalAmount);
    }

    [Fact]
    public async Task GetTotalPerTypeAsync_SumsAmountsPerType()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SampleData());

        var result = await CreateSut().GetTotalPerTypeAsync();

        Assert.Equal(300m, result.Single(r => r.TransactionType == TransactionType.Debit).TotalAmount);
        Assert.Equal(75m, result.Single(r => r.TransactionType == TransactionType.Credit).TotalAmount);
    }

    [Fact]
    public async Task GetHighVolumeAsync_ReturnsAboveThresholdOrderedByAmountDescending()
    {
        const decimal threshold = 40m;
        _repository
            .FindAsync(Arg.Any<Expression<Func<Transaction, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(SampleData().Where(t => t.Amount > threshold).ToList());

        var result = await CreateSut().GetHighVolumeAsync(threshold);

        Assert.Equal(new[] { 200m, 100m, 50m }, result.Select(t => t.Amount));
    }

    [Fact]
    public async Task AddAsync_AddsTransactionAndSavesChanges()
    {
        var dto = new CreateTransactionDto { UserId = "u1", Amount = 10m, TransactionType = TransactionType.Debit };

        var result = await CreateSut().AddAsync(dto);

        await _repository.Received(1).AddAsync(Arg.Is<Transaction>(t => t!.UserId == "u1" && t.Amount == 10m), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("u1", result.UserId);
        Assert.Equal(10m, result.Amount);
    }
}
