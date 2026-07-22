using Challenge.Core.Dtos;

namespace Challenge.Core.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateAsync(string id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
