using Grpc.Core;
using IdentityService.Data;
using IdentityService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Shared.DTOs.Identity;
using Shared.Protos;

namespace IdentityService.Services
{
    [Authorize]
    public class IdentityGrpcService : Shared.Protos.IdentityService.IdentityServiceBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<IdentityGrpcService> _logger;

        public IdentityGrpcService(ISearchService searchService, ILogger<IdentityGrpcService> logger)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(searchService));
        }

        public override async Task<UserInfoResponse> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _searchService.SearchUsersByUserIdAsync(request.UserId);

                if (result.Success && result.Data != null)
                {
                    _logger.LogInformation("Отримано інформацію про користувача {UserId}", request.UserId);

                    return new UserInfoResponse
                    {
                        Success = true,
                        User = new UserDto
                        {
                            Id = result.Data.Id,
                            UserName = result.Data.UserName,
                            DisplayName = result.Data.DisplayName
                        }
                    };
                }
                else
                {
                    return new UserInfoResponse
                    {
                        Success = false,
                        ErrorMessage = $"Користувача з ID {request.UserId} не знайдено"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні інформації про користувача {UserId}", request.UserId);
                return new UserInfoResponse
                {
                    Success = false,
                    ErrorMessage = "Помилка при отриманні даних користувача"
                };
            }
        }

        public override async Task<GetUsersInfoBatchResponse> GetUsersInfoBatch(
            GetUsersInfoBatchRequest request,
            ServerCallContext context)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => request.UserIds.Contains(u.Id))
                    .Select(u => new UserInfo
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        DisplayName = u.DisplayName,
                        Email = u.Email
                    })
                    .ToListAsync();

                var response = new GetUsersInfoBatchResponse
                {
                    Success = true
                };

                foreach (var user in users)
                {
                    response.Users.Add(user.Id, user);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні інформації про користувачів");
                return new GetUsersInfoBatchResponse
                {
                    Success = false,
                    ErrorMessage = "Помилка при отриманні даних користувачів"
                };
            }
        }
    }
}
