namespace MessageService.DTOs
{
    public class SendMessageModel
    {
        // Ідентифікатор чату, в який надсилається повідомлення
        public int ChatRoomId { get; set; }

        // Ідентифікатор відправника
        public int SenderUserId { get; set; }

        // Текст повідомлення
        public string Content { get; set; } = null!;
    }
}
