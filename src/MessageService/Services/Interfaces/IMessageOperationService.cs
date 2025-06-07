using MessageService.Models;
using Shared.DTOs.Message;

namespace MessageService.Services.Interfaces
{
    public interface IMessageOperationService
    {
        Task<MessageOperation> StartOperationAsync(
            Guid correlationId,
            MessageOperationType operationType,
            int userId,
            int? messageId = null,
            int? chatRoomId = null,
            string? operationData = null);

        Task UpdateProgressAsync(Guid correlationId, int progress, string statusMessage);
        Task CompleteOperationAsync(Guid correlationId, string? result = null);
        Task FailOperationAsync(Guid correlationId, string errorMessage, string? errorCode = null);
        Task CancelOperationAsync(Guid correlationId, string reason);
        Task CompensateOperationAsync(Guid correlationId, string reason);
        Task<MessageOperation?> GetOperationAsync(Guid correlationId);
        Task<IEnumerable<MessageOperation>> GetActiveOperationsForUserAsync(int userId);
        Task<IEnumerable<MessageOperation>> GetOperationHistoryForUserAsync(
            int userId, int pageNumber = 1, int pageSize = 20);
        Task<int> GetOperationCountForUserAsync(int userId);
        Task<IEnumerable<MessageOperation>> GetOperationsForChatAsync(int chatRoomId);
        Task<IEnumerable<MessageOperation>> GetOperationsForMessageAsync(int messageId);
        Task<bool> IsOperationActiveAsync(Guid correlationId);
        Task<bool> CanCancelOperationAsync(Guid correlationId);
        Task<bool> IsOperationInProgressAsync(int messageId, MessageOperationType operationType);
        Task<MessageOperation> WaitForOperationCompletionAsync(Guid correlationId, int timeoutSeconds = 30);
    }
}
