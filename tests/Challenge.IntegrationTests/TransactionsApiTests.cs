using System.Net;
using System.Net.Http.Json;
using Challenge.Core.Dtos;
using Challenge.Core.Enums;

namespace Challenge.IntegrationTests;

public class TransactionsApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public TransactionsApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_And_Fetch_User_Works()
    {
        var create = await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = "Grace", Email = "grace@example.com" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(created);

        var fetched = await _client.GetFromJsonAsync<UserDto>($"/api/users/{created!.Id}");
        Assert.NotNull(fetched);
        Assert.Equal("Grace", fetched!.Name);
    }

    [Fact]
    public async Task HighVolume_ReturnsAboveThreshold_OrderedDescending()
    {
        var user = await (await _client.PostAsJsonAsync("/api/users", new CreateUserDto { Name = "Linus", Email = "linus@example.com" }))
            .Content.ReadFromJsonAsync<UserDto>();

        decimal[] amounts = [10m, 500m, 250m, 999m];
        foreach (var amount in amounts)
        {
            await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionDto
            {
                UserId = user!.Id,
                Amount = amount,
                TransactionType = TransactionType.Debit
            });
        }

        var result = await _client.GetFromJsonAsync<List<TransactionDto>>("/api/transactions/high-volume?threshold=200");

        Assert.NotNull(result);
        var highValues = result!.Select(t => t.Amount).ToList();
        Assert.All(highValues, a => Assert.True(a > 200m));
        Assert.Equal(highValues.OrderByDescending(a => a).ToList(), highValues);
    }
}
