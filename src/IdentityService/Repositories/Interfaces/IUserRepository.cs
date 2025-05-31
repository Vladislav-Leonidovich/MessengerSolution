using Shared.DTOs.Identity;

namespace IdentityService.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<UserDto> GetUserByUsernameAsync(string username);
        Task<UserDto> GetUserByUserIdAsync(int userId);
        Task<IEnumerable<UserDto>> GetUsersBatchByUserIdAsync(IEnumerable<int> userIds);
    }
}
