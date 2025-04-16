namespace MessageService.Authorization
{
    public interface IMessageAuthorizationService
    {
        Task<bool> CanAccessMessageAsync(int userId, int messageId);
        Task<bool> CanAccessChatRoomAsync(int userId, int chatRoomId);
        Task EnsureCanAccessMessageAsync(int userId, int messageId);
        Task EnsureCanAccessChatRoomAsync(int userId, int chatRoomId);
    }
}
