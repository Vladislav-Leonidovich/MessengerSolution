using Shared.DTOs.Identity;

namespace IdentityService.Services
{
    public interface ISearchService
    {
        Task<UserDto?> SearchUsersByUsernameAsync(string username);
        Task<UserDto?> SearchUsersByUserIdAsync(int userId);
    }
}
