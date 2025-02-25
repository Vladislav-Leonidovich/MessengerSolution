using ChatServiceDTOs.Chats;
using ChatService.Models;

namespace ChatService.Services
{
    public interface IChatService
    {
        // Створює новий чат з вказаною назвою та списком користувачів
        Task<ChatRoomDto> CreatePrivateChatRoomAsync(CreatePrivateChatRoomDto model);
        Task<GroupChatRoomDto> CreateGroupChatRoomAsync(CreateGroupChatRoomDto model);

        // Отримує список чатів, у яких бере участь вказаний користувач
        Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsForUserAsync();
        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsForUserAsync();
        Task<IEnumerable<ChatRoomDto>> GetPrivateChatsForFolderAsync(int folderId);
        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsForFolderAsync(int folderId);
        Task<IEnumerable<ChatRoomDto>> GetPrivateChatsWithoutFolderAsync();
        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatsWithoutFolderAsync();
    }
}
