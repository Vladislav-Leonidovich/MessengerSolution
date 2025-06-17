using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MessageService.Services.Interfaces;
using Shared.Protos;

namespace MessageService.Services
{
    public class MessageGrpcService : Shared.Protos.MessageGrpcService.MessageGrpcServiceBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessageGrpcService> _logger;

        public MessageGrpcService(
            IMessageService messageService,
            ILogger<MessageGrpcService> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        public override async Task<ApiResponse> GetLastMessage(LastMessageRequest request, ServerCallContext context)
        {
            try
            {
                // Отримання ID користувача з контексту або використання системного ID
                int userId = GetUserIdFromContext(context);

                if (userId == -1)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        ErrorMessage = "Користувач не авторизований"
                    };
                }

                // Виклик сервісу для отримання останнього повідомлення
                var response = await _messageService.GetLastMessagePreviewByChatRoomIdAsync(request.ChatRoomId, userId);

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Отримано останнє повідомлення для чату {ChatRoomId}", request.ChatRoomId);

                    return new ApiResponse
                    {
                        Success = true,
                        Data = new MessageData
                        {
                            Id = response.Data.Id,
                            ChatRoomId = response.Data.ChatRoomId,
                            SenderUserId = response.Data.SenderUserId,
                            Content = response.Data.Content,
                            CreatedAt = Timestamp.FromDateTime(response.Data.CreatedAt.ToUniversalTime()),
                            IsRead = response.Data.IsRead,
                            IsEdited = response.Data.IsEdited
                        }
                    };
                }
                else
                {
                    // Чат існує, але повідомлень немає 
                    if (response.Data != null && string.IsNullOrEmpty(response.Data.Content))
                    {
                        _logger.LogInformation("Чат {ChatRoomId} існує, але повідомлень немає", request.ChatRoomId);
                        return new ApiResponse
                        {
                            Success = true,
                            Data = null // Повертаємо null, якщо повідомлень немає
                        };
                    }
                    // Інші помилки
                    return new ApiResponse
                    {
                        Success = false,
                        Data = null,
                        ErrorMessage = response.Message
                    };
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Користувач не авторизований при отриманні останнього повідомлення для чату {ChatRoomId}",
                    request.ChatRoomId);
                return new ApiResponse
                {
                    Success = false,
                    Data = null,
                    ErrorMessage = "Користувач не авторизований"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останнього повідомлення для чату {ChatRoomId}",
                    request.ChatRoomId);

                return new ApiResponse
                {
                    Success = false,
                    Data = null,
                    ErrorMessage = "Сталася помилка при отриманні останнього повідомлення"
                };
            }
        }

        public override async Task<LastMessagesBatchResponse> GetLastMessagesBatch(LastMessagesBatchRequest request, ServerCallContext context)
        {
            try
            {
                // Отримання ID користувача з контексту або використання системного ID
                int userId = GetUserIdFromContext(context);
                if (userId == -1)
                {
                    return new LastMessagesBatchResponse
                    {
                        Success = false,
                        ErrorMessage = "Користувач не авторизований"
                    };
                }
                // Виклик сервісу для отримання останніх повідомлень
                var response = await _messageService.GetLastMessagesBatchAsync(request.ChatRoomIds, userId);
                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("Отримано пакет останніх повідомлень для чатів {ChatRoomIds}", string.Join(", ", request.ChatRoomIds));

                    var messages = response.Data.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new MessageData
                        {
                            Id = kvp.Value.Id,
                            ChatRoomId = kvp.Value.ChatRoomId,
                            SenderUserId = kvp.Value.SenderUserId,
                            Content = kvp.Value.Content,
                            CreatedAt = Timestamp.FromDateTime(kvp.Value.CreatedAt.ToUniversalTime()),
                            IsRead = kvp.Value.IsRead,
                            IsEdited = kvp.Value.IsEdited
                        });
                    return new LastMessagesBatchResponse
                    {
                        Success = true,
                        Messages = { messages }
                    };
                }
                else
                {
                    return new LastMessagesBatchResponse
                    {
                        Success = false,
                        ErrorMessage = response.Message
                    };
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Користувач не авторизований при отриманні пакету останніх повідомлень");
                return new LastMessagesBatchResponse
                {
                    Success = false,
                    ErrorMessage = "Користувач не авторизований"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні пакету останніх повідомлень");
                return new LastMessagesBatchResponse
                {
                    Success = false,
                    ErrorMessage = "Сталася помилка при отриманні пакету останніх повідомлень"
                };
            }
        }

        // Допоміжний метод для отримання ID користувача з контексту gRPC
        private int GetUserIdFromContext(ServerCallContext context)
        {
            try
            {
                var httpContext = context.GetHttpContext();

                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    throw new UnauthorizedAccessException("Користувач не авторизований");
                }

                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId)
                    ? userId
                    : -1; // Повертаємо -1, якщо ID користувача не знайдено або не є числом
            }
            catch (Exception ex)
            {
                // Загальна обробка помилок при обробці JWT
                _logger.LogError(ex, "Помилка при отриманні ID користувача з JWT токену");
                return -1;
            }
        }
    }
}
