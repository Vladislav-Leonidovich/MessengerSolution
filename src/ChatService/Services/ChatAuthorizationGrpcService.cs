using ChatService.Authorization;
using Shared.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MessageServiceDTOs;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    [Authorize]
    public class ChatAuthorizationGrpcService : Shared.Protos.ChatAuthorizationService.ChatAuthorizationServiceBase
    {
        private readonly IChatAuthorizationService _authService;
        private readonly ILogger<ChatAuthorizationGrpcService> _logger;

        public ChatAuthorizationGrpcService(
            IChatAuthorizationService authService,
            ILogger<ChatAuthorizationGrpcService> logger)
        {
            _authService = authService;
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
                _logger.LogInformation("Отримано gRPC-запит на отримання учасників чату {ChatRoomId}, тип {ChatRoomType}",
                    request.ChatRoomId, request.ChatRoomType);

                // Отримуємо тип чату
                var chatRoomType = (ChatRoomType)request.ChatRoomType;
                var participants = new List<int>();

                // Залежно від типу чату
                if (chatRoomType == ChatRoomType.privateChat)
                {
                    // Отримуємо учасників приватного чату
                    var userChatRooms = await _context.UserChatRooms
                        .Where(ucr => ucr.PrivateChatRoomId == request.ChatRoomId)
                        .ToListAsync();

                    participants = userChatRooms.Select(ucr => ucr.UserId).ToList();
                }
                else if (chatRoomType == ChatRoomType.groupChat)
                {
                    // Отримуємо учасників групового чату
                    var groupChatMembers = await _context.GroupChatMembers
                        .Where(gcm => gcm.GroupChatRoomId == request.ChatRoomId)
                        .ToListAsync();

                    participants = groupChatMembers.Select(gcm => gcm.UserId).ToList();
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
