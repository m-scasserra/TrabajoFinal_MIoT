using Backend.Common.Security;
using Backend.Features.Users.Dtos;

namespace Backend.Features.Users;

public interface IUserService
{
    Task<IEnumerable<UserListItemDto>> ListAsync(CurrentUser me);
    Task<UserListItemDto?> GetByIdAsync(CurrentUser me, Guid id);
    Task<Guid> CreateAsync(CurrentUser me, CreateUserRequest req);
    Task<bool> UpdateAsync(CurrentUser me, Guid id, UpdateUserRequest req);
    Task<bool> DeactivateAsync(CurrentUser me, Guid id);
}