namespace MessageService.Authorization
{
    using MessageService.Data;
    using MessageService.Repositories;
    using MessageService.Repositories.Interfaces;
    using MessageService.Services.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Shared.Authorization;
    using Shared.Authorization.Permissions;
    using Shared.Exceptions;

    namespace ChatService.Authorization
    {
        public class MessagePermissionService : IPermissionService<MessagePermission>
        {
            private readonly IMessageRepository _messageRepository;
            private readonly ILogger<MessagePermissionService> _logger;
            private readonly IChatGrpcClient _chatGrpcClient;

            public MessagePermissionService(IMessageRepository messageRepository, ILogger<MessagePermissionService> logger, IChatGrpcClient chatGrpcClient)
            {
                _messageRepository = messageRepository;
                _logger = logger;
                _chatGrpcClient = chatGrpcClient;
            }

            public async Task<bool> HasPermissionAsync(int userId, MessagePermission permission, int? resourceId = null)
            {
                _logger.LogInformation(
                    "Перевірка дозволу {Permission} для користувача {UserId} та ресурсу {ResourceId}",
                    permission, userId, resourceId);

                switch (permission)
                {
                    case MessagePermission.SendMessage:
                        return await CanSendMessageAsync(userId, resourceId);

                    case MessagePermission.ViewMessage:
                        return await CanViewMessagesAsync(userId, resourceId);
                    case MessagePermission.DeleteMessage:
                        return await CanDeleteMessageAsync(userId, resourceId);
                    case MessagePermission.EditMessage:
                        return await CanEditMessageAsync(userId, resourceId);
                    case MessagePermission.PinMessage:
                        return await CanViewChatAsync(userId, resourceId);
                    case MessagePermission.UnpinMessage:
                        return await CanViewChatAsync(userId, resourceId);
                    case MessagePermission.CleanChat:
                        return await CanViewChatAsync(userId, resourceId);
                    default:
                        _logger.LogWarning("Невідомий дозвіл {Permission}", permission);
                        return false;
                }
            }

            public async Task CheckPermissionAsync(int userId, MessagePermission permission, int? resourceId = null)
            {
                if (!await HasPermissionAsync(userId, permission, resourceId))
                {
                    throw new ForbiddenAccessException(
                        $"У вас немає дозволу {permission} для повідомлення {resourceId}");
                }
            }

            public async Task<bool> CanSendMessageAsync(int userId, int? chatRoomId)
            {
                if (!chatRoomId.HasValue) return false;
                // Перевіряємо, чи є користувач учасником чату
                var canViewChat = await CanViewChatAsync(userId, chatRoomId);
                if (!canViewChat)
                {
                    _logger.LogWarning("Користувач {UserId} не може надіслати повідомлення в чат {ChatRoomId}", userId, chatRoomId);
                    return false;
                }

                return true;
            }

            public async Task<bool> CanViewChatAsync(int userId, int? chatRoomId)
            {
                if (!chatRoomId.HasValue) return false;
                // Перевіряємо, чи є користувач учасником чату
                return await _chatGrpcClient.CheckAccessAsync(userId, chatRoomId.Value);
            }

            public async Task<bool> CanViewMessageAsync(int userId, int? messageId)
            {
                if (!messageId.HasValue) return false;
                // Перевіряємо, чи є користувач учасником чату
                var message = await _messageRepository.GetMessageByIdAsync(messageId.Value);
                if (message == null)
                {
                    _logger.LogWarning("Повідомлення з ID {MessageId} не знайдено", messageId);
                    return false;
                }
                return await CanViewChatAsync(userId, message.ChatRoomId);
            }

            public async Task<bool> CanViewMessagesAsync(int userId, int? chatRoomId)
            {
                if (!chatRoomId.HasValue) return false;

                // Перевіряємо, чи є користувач учасником чату
                return await CanViewChatAsync(userId, chatRoomId);
            }

            public async Task<bool> CanDeleteMessageAsync(int userId, int? messageId)
            {
                if (!messageId.HasValue) return false;
                // Перевіряємо, чи є користувач автором повідомлення
                var message = await _messageRepository.GetMessageByIdAsync(messageId.Value);
                if (message == null)
                {
                    _logger.LogWarning("Повідомлення з ID {MessageId} не знайдено", messageId);
                    return false;
                }
                return message.SenderUserId == userId;
            }

            public async Task<bool> CanEditMessageAsync(int userId, int? messageId)
            {
                if (!messageId.HasValue) return false;
                // Перевіряємо, чи є користувач автором повідомлення
                var message = await _messageRepository.GetMessageByIdAsync(messageId.Value);
                if (message == null)
                {
                    _logger.LogWarning("Повідомлення з ID {MessageId} не знайдено", messageId);
                    return false;
                }
                return message.SenderUserId == userId;
            }
        }
    }

}
