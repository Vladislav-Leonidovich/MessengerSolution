namespace MessageService.Models
{
    public class Message
    {
        public int Id { get; set; }

        // Ідентифікатор чату (особистого або групового)
        public int ChatRoomId { get; set; }

        // Ідентифікатор користувача-відправника
        public int SenderUserId { get; set; }

        // Вміст повідомлення (може бути зашифрованим або звичайним текстом)
        public string Content { get; set; } = null!;

        // Дата і час надсилання повідомлення (UTC)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
