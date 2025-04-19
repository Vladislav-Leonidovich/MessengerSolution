using ChatService.Authorization;
using Shared.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace ChatService.Services
{
    [Authorize]
    public class ChatAuthorizationGrpcService : ChatAuthorizationService.ChatAuthorizationServiceBase
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
    }
}
