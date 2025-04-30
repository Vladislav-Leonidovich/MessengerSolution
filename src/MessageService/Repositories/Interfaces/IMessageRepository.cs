using MassTransit.SagaStateMachine;
using MessageService.Models;
using MessageServiceDTOs;
using Shared.Contracts;

namespace MessageService.Repositories.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<MessageDto>> GetMessagesByChatRoomIdAsync(int chatRoomId, int startIndex, int count);
        Task<MessageDto> CreateMessageForSagaAsync(SendMessageDto model, int userId, Guid correlationId);
        Task<MessageDto> UpdateMessageByIdAsync(int messageId, string newContent);
        Task<int> GetMessagesCountByChatRoomIdAsync(int chatRoomId);
        Task<MessageDto> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId);
        Task DeleteMessageByIdAsync(int messageId);
        Task<bool> DeleteAllMessagesByChatRoomIdAsync(int chatRoomId);
        Task<MessageDto> MarkMessageAsReadByIdAsync(int messageId);
        Task<MessageDto> GetMessageByIdAsync(int messageId);
        Task<MessageDto?> FindMessageByCorrelationIdAsync(Guid correlationId);
    }
}
