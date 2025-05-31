using Shared.DTOs.Identity;

namespace ChatService.Services.Interfaces
{
    public interface IIdentityGrpcService
    {
        Task<UserDto> GetUserInfoAsync(int userId);
        Task<Dictionary<int, UserDto>> GetUsersInfoBatchAsync(IEnumerable<int> userIds);
    }
}
