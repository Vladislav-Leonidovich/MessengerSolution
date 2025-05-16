using MessageServiceDTOs;

namespace MessageService.Models
{
    public class Message
    {
        public int Id { get; set; }

        // Ідентифікатор саги доставки повідомлення
        public Guid? CorrelationId { get; set; }

        // Ідентифікатор чату (особистого або групового)
        public int ChatRoomId { get; set; }

        // Ідентифікатор користувача-відправника
        public int SenderUserId { get; set; }

        // Вміст повідомлення (може бути зашифрованим або звичайним текстом)
        public string Content { get; set; } = null!;

        // Дата і час надсилання повідомлення (UTC)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Чи було повідомлення прочитане
        public bool IsRead { get; set; } = false;

        // Дата та час, коли повідомлення було прочитане (якщо IsRead == true)
        public DateTime? ReadAt { get; set; }
        // Чи було повідомлення прочитане
        public bool IsEdited { get; set; } = false;

        // Дата редагування повідомлення (якщо воно було змінене)
        public DateTime? EditedAt { get; set; }
    }
}
