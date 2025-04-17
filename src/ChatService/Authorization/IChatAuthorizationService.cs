namespace ChatService.Authorization
{
    public interface IChatAuthorizationService
    {
        Task<bool> CanAccessChatRoomAsync(int userId, int chatRoomId);
        Task<bool> CanAccessFolderAsync(int userId, int folderId);
        Task<bool> CanModifyChatAsync(int userId, int chatRoomId);
        Task<bool> CanAddUserToChatAsync(int userId, int chatRoomId, int targetUserId);
        Task EnsureCanAccessChatRoomAsync(int userId, int chatRoomId); // Виняток, якщо немає доступу
        Task EnsureCanAccessFolderAsync(int userId, int folderId); // Виняток, якщо немає доступу
    }
}
