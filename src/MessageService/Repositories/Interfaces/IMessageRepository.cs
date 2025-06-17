using MassTransit.SagaStateMachine;
using MessageService.Models;
using Shared.Contracts;
using Shared.DTOs.Common;
using Shared.DTOs.Message;

namespace MessageService.Repositories.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<MessageDto>> GetMessagesByChatRoomIdAsync(int chatRoomId, int startIndex, int count);
        Task<MessageDto> CreateMessageAsync(string content, int userId, Guid correlationId, int chatRoomId);
        Task<MessageDto> UpdateMessageByIdAsync(int messageId, string newContent);
        Task<int> GetMessagesCountByChatRoomIdAsync(int chatRoomId);
        Task<MessageDto> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId);
        Task<Dictionary<int, MessageDto>> GetLastMessagePreviewBatchByChatRoomIdAsync(IEnumerable<int> chatRoomIds);
        Task DeleteMessageByIdAsync(int messageId);
        Task<(bool Success, int DeletedCount)> DeleteAllMessagesByChatRoomIdAsync(int chatRoomId);
        Task<MessageDto> MarkMessageAsReadByIdAsync(int messageId);
        Task<MessageDto> GetMessageByIdAsync(int messageId);
        Task<MessageDto?> FindMessageByCorrelationIdAsync(Guid correlationId);
        Task<int> GetUserIdSenderMessageAsync(int messageId);
        Task<Guid?> GetCorrelationIdByMessageIdAsync(int messageId);
        Task UpdateMessageStatusAsync(int messageId, MessageStatus status);
        Task UpdateMessageStatusByCorrelationIdAsync(Guid correlationId, MessageStatus status);
        Task AddToOutboxAsync(string eventType, object eventData);
    }
}
