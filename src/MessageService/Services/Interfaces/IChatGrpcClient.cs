namespace MessageService.Services.Interfaces
{
    public interface IChatGrpcClient
    {
        Task<bool> CheckAccessAsync(int userId, int chatRoomId);
        Task<Dictionary<(int UserId, int ChatRoomId), bool>> CheckAccessBatchAsync(
            List<(int UserId, int ChatRoomId)> checks);
    }
}
