﻿using Grpc.Core;
using IdentityService.Data;
using IdentityService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Shared.DTOs.Identity;
using Shared.Protos;

namespace IdentityService.Services
{
    [Authorize]
    public class IdentityGrpcService : Shared.Protos.IdentityGrpcService.IdentityGrpcServiceBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<IdentityGrpcService> _logger;

        public IdentityGrpcService(ISearchService searchService, ILogger<IdentityGrpcService> logger)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(searchService));
        }

        public async override Task<UserInfoResponse> GetUserInfoByUserId(GetUserInfoRequest request, ServerCallContext context)
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
                        User = new UserData
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

        public async override Task<GetUsersInfoBatchResponse> GetUsersInfoByUserIdBatch(
            GetUsersInfoBatchRequest request,
            ServerCallContext context)
        {
            try
            {
                var users = await _searchService.SearchUsersBatchByUserIdAsync(request.UserIds);
                if (users == null ||  users.Data == null || !users.Data.Any())
                {
                    _logger.LogWarning("Не знайдено користувачів для ID: {UserIds}", string.Join(", ", request.UserIds));
                    return new GetUsersInfoBatchResponse
                    {
                        Success = false,
                        ErrorMessage = "Користувачі не знайдені"
                    };
                }
                var response = new GetUsersInfoBatchResponse
                {
                    Success = true
                };

                foreach (var user in users.Data)
                {
                    response.Users.Add(user.Id, new UserData
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        DisplayName = user.DisplayName
                    });
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
