using ChatService.Services.Interfaces;
using Grpc.Core;
using Shared.DTOs.Identity;

namespace ChatService.Services
{
    public class IdentityGrpcService : IIdentityGrpcService
    {
        private readonly IdentityGrpcService.IdentityGrpcServiceClient _client;
        private readonly ILogger<IdentityGrpcService> _logger;

        public IdentityGrpcService(
            IdentityGrpcService.IdentityGrpcServiceClient client,
            ILogger<IdentityGrpcService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<UserDto?> GetUserInfoAsync(int userId)
        {
            try
            {
                var request = new GetUserInfoRequest { UserId = userId };
                var response = await _client.GetUserInfoAsync(request);

                return response.Success ? response.User : null;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning("Користувача {UserId} не знайдено", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні інформації про користувача {UserId}", userId);
                return null;
            }
        }

        public async Task<Dictionary<int, UserDto>>? GetUsersInfoBatchAsync(IEnumerable<int> userIds)
        {
            try
            {
                var request = new GetUsersInfoBatchRequest();
                request.UserIds.AddRange(userIds);

                var response = await _client.GetUsersInfoBatchAsync(request);

                return response.Success
                    ? response.Users.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    : new Dictionary<int, UserDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні інформації про користувачів");
                return new Dictionary<int, UserDto>();
            }
        }
    }
}
