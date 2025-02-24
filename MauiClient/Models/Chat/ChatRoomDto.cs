using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiClient.Models.Message;

namespace MauiClient.Models.Chat
{
    public class ChatRoomDto
    {
        // Ідентифікатор чату
        public int Id { get; set; }
        // Назва чату
        public string Name { get; set; } = string.Empty;
        // Дата створення чату
        public DateTime CreatedAt { get; set; }
        // Список ідентифікаторів користувачів, що беруть участь у чаті
        public List<int> UserIds { get; set; } = new List<int>();
        // Список повідомлень у чаті (за потреби)
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
    }
}
