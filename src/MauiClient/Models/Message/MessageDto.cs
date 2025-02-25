using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiClient.Models.Message
{
    // Модель повідомлення
    public class MessageDto
    {
        // Ідентифікатор повідомлення
        public int Id { get; set; }
        // Ідентифікатор чату, до якого належить повідомлення
        public int ChatRoomId { get; set; }
        // Ідентифікатор користувача, який відправив повідомлення
        public int SenderId { get; set; }
        // Текст повідомлення
        public string Content { get; set; } = string.Empty;
        // Дата та час надсилання повідомлення
        public DateTime CreatedAt { get; set; }
    }
}
