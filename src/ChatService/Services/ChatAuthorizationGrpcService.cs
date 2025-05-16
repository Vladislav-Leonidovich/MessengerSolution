using ChatService.Authorization;
using Shared.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MessageServiceDTOs;
using Microsoft.EntityFrameworkCore;
using ChatService.Repositories;
using ChatService.Repositories.Interfaces;

namespace ChatService.Services
{
    [Authorize]
    public class ChatAuthorizationGrpcService : Shared.Protos.ChatAuthorizationService.ChatAuthorizationServiceBase
    {
        private readonly IChatAuthorizationService _authService;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly ILogger<ChatAuthorizationGrpcService> _logger;

        public ChatAuthorizationGrpcService(
            IChatAuthorizationService authService,
            IChatRoomRepository chatRoomRepository,
            ILogger<ChatAuthorizationGrpcService> logger)
        {
            _authService = authService;
            _chatRoomRepository = chatRoomRepository;
            _logger = logger;
        }

        public override async Task<CheckAccessResponse> CheckAccess(
            CheckAccessRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Отримано gRPC-запит на перевірку доступу користувача {UserId} до чату {ChatRoomId}",
                    request.UserId, request.ChatRoomId);

                var hasAccess = await _authService.CanAccessChatRoomAsync(
                    request.UserId, request.ChatRoomId);

                return new CheckAccessResponse
                {
                    HasAccess = hasAccess
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при перевірці доступу користувача {UserId} до чату {ChatRoomId}",
                    request.UserId, request.ChatRoomId);

                return new CheckAccessResponse
                {
                    HasAccess = false,
                    ErrorMessage = "Виникла помилка під час перевірки доступу"
                };
            }
        }

        public override async Task<CheckAccessBatchResponse> CheckAccessBatch(
            CheckAccessBatchRequest request, ServerCallContext context)
        {
            var response = new CheckAccessBatchResponse();

            foreach (var check in request.Checks)
            {
                try
                {
                    var hasAccess = await _authService.CanAccessChatRoomAsync(
                        check.UserId, check.ChatRoomId);

                    response.Results.Add(new ChatAccessResult
                    {
                        UserId = check.UserId,
                        ChatRoomId = check.ChatRoomId,
                        HasAccess = hasAccess
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка при пакетній перевірці доступу користувача {UserId} до чату {ChatRoomId}",
                        check.UserId, check.ChatRoomId);

                    response.Results.Add(new ChatAccessResult
                    {
                        UserId = check.UserId,
                        ChatRoomId = check.ChatRoomId,
                        HasAccess = false
                    });
                }
            }

            return response;
        }

        public override async Task<GetChatParticipantsResponse> GetChatParticipants(
        GetChatParticipantsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Отримано gRPC-запит на отримання учасників чату {ChatRoomId}",
                    request.ChatRoomId);

                // Отримуємо тип чату
                var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(request.ChatRoomId);
                var participants = new List<int>();

                switch(chatRoomType)
                {
                    case ChatRoomType.privateChat:
                        // Отримуємо учасників приватного чату
                        participants = await _chatRoomRepository.GetChatParticipantsFromPrivateChatAsync(request.ChatRoomId);
                        break;
                    case ChatRoomType.groupChat:
                        // Отримуємо учасників групового чату
                        participants = await _chatRoomRepository.GetChatParticipantsFromGroupChatAsync(request.ChatRoomId);
                        break;
                    default:
                        return new GetChatParticipantsResponse
                        {
                            Success = false,
                            ErrorMessage = "Невідомий тип чату"
                        };
                }

                return new GetChatParticipantsResponse
                {
                    Success = true,
                    ParticipantIds = { participants }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні учасників чату {ChatRoomId}", request.ChatRoomId);

                return new GetChatParticipantsResponse
                {
                    Success = false,
                    ErrorMessage = "Помилка при отриманні учасників чату"
                };
            }
        }
    }
}
