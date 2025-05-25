using ChatService.Models;
using Shared.ChatServiceDTOs.Chats;

namespace ChatService.Services.Interfaces
{
    public interface IChatOperationService
    {
        Task<ChatOperation> StartOperationAsync(
            Guid correlationId, 
            ChatOperationType operationType, 
            int chatRoomId, 
            int userId, 
            string? operationData = null);
        Task UpdateProgressAsync(Guid correlationId, int progress, string statusMessage);
        Task CompleteOperationAsync(Guid correlationId, string? result = null);
        Task FailOperationAsync(Guid correlationId, string errorMessage, string? errorCode = null);
        Task CancelOperationAsync(Guid correlationId, string reason);
        Task CompensateOperationAsync(Guid correlationId, string reason);
        Task<ChatOperation?> GetOperationAsync(Guid correlationId);
        Task<IEnumerable<ChatOperation>> GetOperationHistoryForUserAsync(
            int userId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<ChatOperation>> GetActiveOperationsForChatAsync(int chatRoomId);
        Task<int> GetOperationCountForUserAsync(int userId);
        Task<IEnumerable<ChatOperation>> GetOperationsForChatAsync(int chatRoomId);
        Task<bool> IsOperationActiveAsync(Guid correlationId);
        Task<bool> CanCancelOperationAsync(Guid correlationId);
        Task<bool> IsOperationInProgressAsync(int chatRoomId, ChatOperationType operationType);
    }
}
