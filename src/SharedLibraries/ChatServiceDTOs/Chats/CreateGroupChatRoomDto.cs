using System.Collections.Generic;

namespace ChatService.DTOs
{
    // DTO для створення групового чату
    public class CreateGroupChatRoomDto
    {
        // Назва групового чату (встановлюється власником)
        public string Name { get; set; } = null!;
        // Список ID учасників групи (без власника, який визначається автоматично)
        public List<int> MemberIds { get; set; } = new List<int>();
    }
}
