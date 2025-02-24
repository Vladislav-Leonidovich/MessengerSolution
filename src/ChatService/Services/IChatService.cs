using ChatService.DTOs;
using ChatService.Models;

namespace ChatService.Services
{
    public interface IChatService
    {
        // Створює новий чат з вказаною назвою та списком користувачів
        Task<ChatRoomDto> CreateChatRoomAsync(CreateChatRoomDto model);

        // Отримує список чатів, у яких бере участь вказаний користувач
        Task<IEnumerable<ChatRoomDto>> GetChatRoomsForUserAsync();
        Task<IEnumerable<ChatRoomDto>> GetChatsForFolder(int folderId);
        Task<IEnumerable<ChatRoomDto>> GetChatsWithoutFolder();
    }
}
