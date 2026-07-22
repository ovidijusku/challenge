using System.Net;
using System.Net.Http.Json;
using Challenge.Core.Dtos;
using Challenge.Core.Enums;

namespace Challenge.IntegrationTests;

/// <summary>
/// End-to-end tests for the error/constraint contracts that only a real SQL Server can
/// enforce (unique index, foreign-key restrict) plus the endpoints not covered elsewhere.
/// Runs against its own disposable database, so tests share state and rely on unique
/// e-mail addresses / per-user filtering to stay independent of one another.
/// </summary>
public class ApiEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<UserDto> CreateUserAsync(string name, string email)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = name, Email = email });
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        return user!;
    }

    private async Task AddTransactionAsync(string userId, decimal amount, TransactionType type)
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionDto
        {
            UserId = userId,
            Amount = amount,
            TransactionType = type,
        });
        response.EnsureSuccessStatusCode();
    }

    // ---------------------------------------------------------------- Users

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        const string email = "duplicate@example.com";
        await CreateUserAsync("First", email);

        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = "Second", Email = email });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = "Bad", Email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithMissingName_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = "", Email = "missing-name@example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenExists_ReturnsOkAndSetsUpdatedAt()
    {
        var user = await CreateUserAsync("Original", "update-me@example.com");

        var response = await _client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new UpdateUserDto { Name = "Renamed", Email = "renamed@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(updated);
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal("renamed@example.com", updated.Email);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateUser_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/users/{Guid.NewGuid()}",
            new UpdateUserDto { Name = "X", Email = "missing-update@example.com" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithoutTransactions_ReturnsNoContent()
    {
        var user = await CreateUserAsync("Deletable", "deletable@example.com");

        var response = await _client.DeleteAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetch = await _client.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithTransactions_ReturnsConflictAndKeepsRecords()
    {
        var user = await CreateUserAsync("Protected", "protected@example.com");
        await AddTransactionAsync(user.Id, 25m, TransactionType.Debit);

        var response = await _client.DeleteAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // The user and its transactions must survive the blocked delete.
        var fetch = await _client.GetAsync($"/api/users/{user.Id}");
        Assert.Equal(HttpStatusCode.OK, fetch.StatusCode);

        var transactions = await _client.GetFromJsonAsync<List<TransactionDto>>($"/api/transactions?userId={user.Id}");
        Assert.NotNull(transactions);
        Assert.Single(transactions!);
    }

    // --------------------------------------------------------- Transactions

    [Fact]
    public async Task AddTransaction_ForMissingUser_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionDto
        {
            UserId = Guid.NewGuid().ToString(),
            Amount = 100m,
            TransactionType = TransactionType.Credit,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddTransaction_WithNonPositiveAmount_ReturnsBadRequest()
    {
        var user = await CreateUserAsync("ZeroAmount", "zero-amount@example.com");

        var response = await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionDto
        {
            UserId = user.Id,
            Amount = 0m,
            TransactionType = TransactionType.Debit,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_FilteredByUser_ReturnsOnlyThatUsers()
    {
        var mine = await CreateUserAsync("Mine", "mine@example.com");
        var other = await CreateUserAsync("Other", "other@example.com");
        await AddTransactionAsync(mine.Id, 10m, TransactionType.Debit);
        await AddTransactionAsync(mine.Id, 20m, TransactionType.Credit);
        await AddTransactionAsync(other.Id, 30m, TransactionType.Debit);

        var result = await _client.GetFromJsonAsync<List<TransactionDto>>($"/api/transactions?userId={mine.Id}");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.All(result, t => Assert.Equal(mine.Id, t.UserId));
    }

    [Fact]
    public async Task TotalsPerUser_IncludesUserWithZeroTransactions()
    {
        var withoutTx = await CreateUserAsync("NoTx", "no-tx@example.com");
        var withTx = await CreateUserAsync("WithTx", "with-tx@example.com");
        await AddTransactionAsync(withTx.Id, 40m, TransactionType.Debit);
        await AddTransactionAsync(withTx.Id, 60m, TransactionType.Credit);

        var totals = await _client.GetFromJsonAsync<List<UserTotalDto>>("/api/transactions/totals/per-user");

        Assert.NotNull(totals);
        Assert.Equal(0m, totals!.Single(t => t.UserId == withoutTx.Id).TotalAmount);
        Assert.Equal(100m, totals.Single(t => t.UserId == withTx.Id).TotalAmount);
    }

    [Fact]
    public async Task TotalsPerType_ReflectsPostedAmounts()
    {
        var before = await _client.GetFromJsonAsync<List<TransactionTypeTotalDto>>("/api/transactions/totals/per-type");
        var creditBefore = before!.SingleOrDefault(t => t.TransactionType == TransactionType.Credit)?.TotalAmount ?? 0m;

        var user = await CreateUserAsync("TypeTotals", "type-totals@example.com");
        await AddTransactionAsync(user.Id, 100m, TransactionType.Credit);
        await AddTransactionAsync(user.Id, 50m, TransactionType.Credit);

        var after = await _client.GetFromJsonAsync<List<TransactionTypeTotalDto>>("/api/transactions/totals/per-type");
        var creditAfter = after!.Single(t => t.TransactionType == TransactionType.Credit).TotalAmount;

        Assert.Equal(creditBefore + 150m, creditAfter);
    }
}
