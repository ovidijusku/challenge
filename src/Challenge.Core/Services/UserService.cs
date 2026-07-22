using AutoMapper;
using Challenge.Core.Dtos;
using Challenge.Core.Entities;
using Challenge.Core.Interfaces;

namespace Challenge.Core.Services;

public class UserService(IRepository<User> users, IMapper mapper) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await users.GetAllAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<UserDto>>(result);
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null ? null : mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = mapper.Map<User>(dto);
        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        mapper.Map(dto, user);
        user.UpdatedAt = DateTime.UtcNow;
        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);
        return mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        users.Remove(user);
        await users.SaveChangesAsync(cancellationToken);
        return true;
    }
}
