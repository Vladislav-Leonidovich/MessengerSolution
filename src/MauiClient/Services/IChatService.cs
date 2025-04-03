using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatServiceDTOs.Chats;

namespace MauiClient.Services
{
    // Інтерфейс для роботи з чатами
    public interface IChatService
    {
        Task<ChatRoomDto?> CreatePrivateChatRoomAsync(CreatePrivateChatRoomDto model);

        Task<GroupChatRoomDto?> CreateGroupChatRoomAsync(CreateGroupChatRoomDto model);

        Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsWithoutFolderAsync();

        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsWithoutFolderAsync();

        Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsForFolderAsync(int folderId);

        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsForFolderAsync(int folderId);

        Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsAsync();

        Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsAsync();

        Task<ChatRoomDto> GetPrivateChatRoomAsync(int chatId);

        Task<GroupChatRoomDto> GetGroupChatRoomAsync(int chatId);

        Task<bool> DeletePrivateChatRoomAsync(int chatId);

        Task<bool> DeleteGroupChatRoomAsync(int chatId);
    }
}
