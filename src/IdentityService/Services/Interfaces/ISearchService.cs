using Shared.DTOs.Identity;
using Shared.DTOs.Responses;

namespace IdentityService.Services.Interfaces
{
    public interface ISearchService
    {
        Task<ApiResponse<UserDto>> SearchUsersByUsernameAsync(string username);
        Task<ApiResponse<UserDto>> SearchUsersByUserIdAsync(int userId);
        Task<ApiResponse<IEnumerable<UserDto>>> SearchUsersBatchByUserIdAsync(IEnumerable<int> userIds);
    }
}
