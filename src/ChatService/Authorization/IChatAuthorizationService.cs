namespace ChatService.Authorization
{
    public interface IChatAuthorizationService
    {
        Task<bool> CanAccessChatRoom(int userId, int chatRoomId);
        Task<bool> CanAccessFolder(int userId, int folderId);
        Task<bool> CanModifyChat(int userId, int chatRoomId);
        Task<bool> CanAddUserToChat(int userId, int chatRoomId, int targetUserId);
        Task EnsureCanAccessChatRoom(int userId, int chatRoomId); // Виняток, якщо немає доступу
        Task EnsureCanAccessFolder(int userId, int folderId); // Виняток, якщо немає доступу
    }
}
