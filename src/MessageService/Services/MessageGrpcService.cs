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

        // Допоміжний метод для отримання ID користувача з контексту gRPC
        private int GetUserIdFromContext(ServerCallContext context)
        {
            try
            {
                // Отримуємо Authorization хедер
                var authHeader = context.RequestHeaders.FirstOrDefault(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))?.Value;

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("JWT токен відсутній або має неправильний формат");
                    return -1;
                }

                // Вилучаємо JWT токен з Bearer префіксу
                var token = authHeader.Substring("Bearer ".Length).Trim();

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("JWT токен порожній після обрізання префіксу");
                    return -1;
                }

                // Декодуємо JWT токен
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                {
                    _logger.LogWarning("Неможливо розпізнати JWT токен");
                    return -1;
                }

                var jwtToken = handler.ReadJwtToken(token);

                // Шукаємо claim з ID користувача
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c =>
                    c.Type == ClaimTypes.NameIdentifier ||
                    c.Type == "sub" ||
                    c.Type == "userId");

                if (userIdClaim == null)
                {
                    _logger.LogWarning("Claim з ID користувача не знайдено в JWT токені");
                    return -1;
                }

                // Конвертуємо ID користувача в int
                if (int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogDebug("Успішно отримано ID користувача {UserId} з JWT токену", userId);
                    return userId;
                }
                else
                {
                    _logger.LogWarning("Неможливо конвертувати ID користувача '{RawUserId}' в число", userIdClaim.Value);
                    return -1;
                }
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
