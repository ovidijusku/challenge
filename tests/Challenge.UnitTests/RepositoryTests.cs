using Challenge.Core.Entities;
using Challenge.Infrastructure.Data;
using Challenge.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Challenge.UnitTests;

public sealed class RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Repository<User> _sut;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"repo-tests-{Guid.NewGuid()}")
            .Options;
        _context = new AppDbContext(options);
        _sut = new Repository<User>(_context);
    }

    private async Task<User> SeedUserAsync(string name = "Ada", string email = "ada@example.com")
    {
        var user = new User { Name = name, Email = email };
        await _sut.AddAsync(user);
        await _sut.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsEntity()
    {
        var user = await SeedUserAsync();

        Assert.Equal(1, await _context.Users.CountAsync());
        Assert.Equal(user.Id, (await _context.Users.SingleAsync()).Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        await SeedUserAsync("Ada", "ada@example.com");
        await SeedUserAsync("Alan", "alan@example.com");

        var all = await _sut.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEntity()
    {
        var user = await SeedUserAsync();

        var found = await _sut.GetByIdAsync(user.Id);

        Assert.NotNull(found);
        Assert.Equal("Ada", found!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var found = await _sut.GetByIdAsync("missing");

        Assert.Null(found);
    }

    [Fact]
    public async Task FindAsync_ReturnsOnlyMatchingEntities()
    {
        await SeedUserAsync("Ada", "ada@example.com");
        await SeedUserAsync("Alan", "alan@example.com");

        var result = await _sut.FindAsync(u => u.Name == "Ada");

        Assert.Single(result);
        Assert.Equal("Ada", result[0].Name);
    }

    [Fact]
    public async Task Update_ThenSaveChanges_PersistsChanges()
    {
        var user = await SeedUserAsync();

        user.Name = "Ada Lovelace";
        _sut.Update(user);
        await _sut.SaveChangesAsync();

        Assert.Equal("Ada Lovelace", (await _context.Users.SingleAsync()).Name);
    }

    [Fact]
    public async Task Remove_ThenSaveChanges_DeletesEntity()
    {
        var user = await SeedUserAsync();

        _sut.Remove(user);
        await _sut.SaveChangesAsync();

        Assert.Equal(0, await _context.Users.CountAsync());
    }

    public void Dispose() => _context.Dispose();
}
