using ChatService.Mappers.Interfaces;
using ChatService.Services.Interfaces;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Shared.DTOs.Identity;
using Shared.Exceptions;
using Shared.Protos;

namespace ChatService.Services
{
    [Authorize]
    public class IdentityGrpcService : IIdentityGrpcService
    {
        private readonly Shared.Protos.IdentityGrpcService.IdentityGrpcServiceClient _client;
        private readonly IMapperFactory _mapperFactory;
        private readonly ILogger<IdentityGrpcService> _logger;

        public IdentityGrpcService(
            Shared.Protos.IdentityGrpcService.IdentityGrpcServiceClient client,
            IMapperFactory mapperFactory,
            ILogger<IdentityGrpcService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(mapperFactory));
            _mapperFactory = mapperFactory ?? throw new ArgumentNullException(nameof(mapperFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(mapperFactory));
        }

        public async Task<UserDto> GetUserInfoAsync(int userId)
        {
            try
            {
                var request = new GetUserInfoRequest { UserId = userId };
                var response = await _client.GetUserInfoByUserIdAsync(request);

                return response.Success ? await _mapperFactory.GetMapper<UserData, UserDto>().MapToDtoAsync(response.User) : throw new EntityNotFoundException("UserData", userId);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning("Користувача {UserId} не знайдено", userId);
                throw;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Помилка gRPC при отриманні інформації про користувача {UserId}", userId);
                throw;
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex, "Користувач з ID {UserId} не знайдений", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні інформації про користувача {UserId}", userId);
                throw;
            }
        }

        public async Task<Dictionary<int, UserDto>> GetUsersInfoBatchAsync(IEnumerable<int> userIds)
        {
            try
            {
                var request = new GetUsersInfoBatchRequest();
                request.UserIds.AddRange(userIds);

                var response = await _client.GetUsersInfoByUserIdBatchAsync(request);
                var result = new Dictionary<int, UserDto>();
                if (response == null)
                {
                    _logger.LogWarning("Отримано null відповідь від gRPC сервісу для GetUsersInfoBatchAsync");
                    throw new EntityNotFoundException("UserData", "Batch request returned null response");
                }
                foreach (var kvp in response.Users)
                {
                    if (kvp.Value != null)
                    {
                        var userDto = await _mapperFactory.GetMapper<UserData, UserDto>().MapToDtoAsync(kvp.Value);
                        result.Add(kvp.Key, userDto);
                    }
                    else
                    {
                        _logger.LogWarning("UserData для ID {UserId} не знайдено в відповіді gRPC", kvp.Key);
                    }
                }
                return result;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Помилка gRPC батч-запиту користувачів: {Status} - {Message}",
                    ex.StatusCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні інформації про користувачів");
                throw;
            }
        }
    }
}
