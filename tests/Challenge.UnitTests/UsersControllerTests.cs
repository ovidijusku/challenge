using Challenge.Api.Controllers;
using Challenge.Core.Dtos;
using Challenge.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Challenge.UnitTests;

public class UsersControllerTests
{
    private readonly IUserService _service = Substitute.For<IUserService>();

    private UsersController CreateSut() => new(_service);

    [Fact]
    public async Task GetAll_ReturnsOkWithUsers()
    {
        IReadOnlyList<UserDto> users = [new UserDto { Id = "u1", Name = "Ada", Email = "ada@example.com" }];
        _service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(users);

        var result = await CreateSut().GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(users, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var user = new UserDto { Id = "u1", Name = "Ada", Email = "ada@example.com" };
        _service.GetByIdAsync("u1", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().GetById("u1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(user, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        _service.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((UserDto?)null);

        var result = await CreateSut().GetById("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithLocation()
    {
        var created = new UserDto { Id = "u1", Name = "Ada", Email = "ada@example.com" };
        _service.CreateAsync(Arg.Any<CreateUserDto>(), Arg.Any<CancellationToken>()).Returns(created);

        var result = await CreateSut().Create(new CreateUserDto { Name = "Ada", Email = "ada@example.com" }, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(created, createdResult.Value);
        Assert.Equal("u1", createdResult.RouteValues!["id"]);
    }

    [Fact]
    public async Task Update_WhenFound_ReturnsOk()
    {
        var updated = new UserDto { Id = "u1", Name = "New", Email = "new@example.com" };
        _service.UpdateAsync("u1", Arg.Any<UpdateUserDto>(), Arg.Any<CancellationToken>()).Returns(updated);

        var result = await CreateSut().Update("u1", new UpdateUserDto { Name = "New", Email = "new@example.com" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        _service.UpdateAsync("missing", Arg.Any<UpdateUserDto>(), Arg.Any<CancellationToken>()).Returns((UserDto?)null);

        var result = await CreateSut().Update("missing", new UpdateUserDto { Name = "New", Email = "new@example.com" }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WhenDeleted_ReturnsNoContent()
    {
        _service.DeleteAsync("u1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Delete("u1", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        _service.DeleteAsync("missing", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Delete("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
