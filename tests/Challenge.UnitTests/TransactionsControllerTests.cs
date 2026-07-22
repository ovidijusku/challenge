using Challenge.Api.Controllers;
using Challenge.Core.Dtos;
using Challenge.Core.Enums;
using Challenge.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Challenge.UnitTests;

public class TransactionsControllerTests
{
    private readonly ITransactionService _service = Substitute.For<ITransactionService>();

    private TransactionsController CreateSut() => new(_service);

    [Fact]
    public async Task Add_ReturnsCreatedAtAction()
    {
        var created = new TransactionDto { Id = 1, UserId = "u1", Amount = 10m, TransactionType = TransactionType.Debit };
        _service.AddAsync(Arg.Any<CreateTransactionDto>(), Arg.Any<CancellationToken>()).Returns(created);

        var result = await CreateSut().Add(
            new CreateTransactionDto { UserId = "u1", Amount = 10m, TransactionType = TransactionType.Debit },
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task Add_WhenUserDoesNotExist_ReturnsBadRequest()
    {
        _service.AddAsync(Arg.Any<CreateTransactionDto>(), Arg.Any<CancellationToken>()).Returns((TransactionDto?)null);

        var result = await CreateSut().Add(
            new CreateTransactionDto { UserId = "missing", Amount = 10m, TransactionType = TransactionType.Debit },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutUserId_ReturnsAllTransactions()
    {
        IReadOnlyList<TransactionDto> all = [new TransactionDto { Id = 1, UserId = "u1", Amount = 10m }];
        _service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(all);

        var result = await CreateSut().GetAll(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(all, ok.Value);
        await _service.DidNotReceive().GetByUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_WithUserId_FiltersByUser()
    {
        IReadOnlyList<TransactionDto> forUser = [new TransactionDto { Id = 1, UserId = "u1", Amount = 10m }];
        _service.GetByUserAsync("u1", Arg.Any<CancellationToken>()).Returns(forUser);

        var result = await CreateSut().GetAll("u1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(forUser, ok.Value);
        await _service.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTotalsPerUser_ReturnsOk()
    {
        IReadOnlyList<UserTotalDto> totals = [new UserTotalDto { UserId = "u1", TotalAmount = 150m }];
        _service.GetTotalPerUserAsync(Arg.Any<CancellationToken>()).Returns(totals);

        var result = await CreateSut().GetTotalsPerUser(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(totals, ok.Value);
    }

    [Fact]
    public async Task GetTotalsPerType_ReturnsOk()
    {
        IReadOnlyList<TransactionTypeTotalDto> totals =
            [new TransactionTypeTotalDto { TransactionType = TransactionType.Debit, TotalAmount = 300m }];
        _service.GetTotalPerTypeAsync(Arg.Any<CancellationToken>()).Returns(totals);

        var result = await CreateSut().GetTotalsPerType(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(totals, ok.Value);
    }

    [Fact]
    public async Task GetHighVolume_PassesThresholdToService()
    {
        IReadOnlyList<TransactionDto> high = [new TransactionDto { Id = 1, UserId = "u1", Amount = 200m }];
        _service.GetHighVolumeAsync(50m, Arg.Any<CancellationToken>()).Returns(high);

        var result = await CreateSut().GetHighVolume(50m, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(high, ok.Value);
        await _service.Received(1).GetHighVolumeAsync(50m, Arg.Any<CancellationToken>());
    }
}
