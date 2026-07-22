using AutoMapper;
using Challenge.Core.Dtos;
using Challenge.Core.Entities;
using Challenge.Core.Interfaces;
using Challenge.Core.Services;
using NSubstitute;

namespace Challenge.UnitTests;

public class UserServiceTests
{
    private readonly IRepository<User> _repository = Substitute.For<IRepository<User>>();
    private readonly IMapper _mapper = TestMapperFactory.Create();

    private UserService CreateSut() => new(_repository, _mapper);

    [Fact]
    public async Task CreateAsync_AddsUserAndReturnsDto()
    {
        var dto = new CreateUserDto { Name = "Ada", Email = "ada@example.com" };

        var result = await CreateSut().CreateAsync(dto);

        await _repository.Received(1).AddAsync(Arg.Is<User>(u => u!.Name == "Ada" && u.Email == "ada@example.com"), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Ada", result.Name);
        Assert.False(string.IsNullOrWhiteSpace(result.Id));
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().GetByIdAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsDto()
    {
        var existing = new User { Id = "u1", Name = "Old", Email = "old@example.com" };
        _repository.GetByIdAsync("u1", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CreateSut().UpdateAsync("u1", new UpdateUserDto { Name = "New", Email = "new@example.com" });

        _repository.Received(1).Update(existing);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.NotNull(result);
        Assert.Equal("New", result!.Name);
        Assert.Equal("new@example.com", result.Email);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ReturnsFalse()
    {
        _repository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().DeleteAsync("missing");

        Assert.False(result);
        _repository.DidNotReceive().Remove(Arg.Any<User>());
    }
}
