using Shared.DTOs.Identity;

namespace IdentityService.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<UserDto> GetUsersByUsernameAsync(string username);
        Task<UserDto> GetUsersByUserIdAsync(int userId);
    }
}
