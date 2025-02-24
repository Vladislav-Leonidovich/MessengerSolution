using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiClient.Models.Chat;

namespace MauiClient.Services
{
    // Інтерфейс для роботи з чатами
    public interface IChatService
    {
        Task<IEnumerable<ChatRoomDto>> GetChatRoomsAsync(int userId);
        Task<ChatRoomDto?> CreateChatRoomAsync(CreateChatRoomDto model);
    }
}
