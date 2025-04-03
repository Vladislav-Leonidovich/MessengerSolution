namespace MessageServiceDTOs
{
    public class SendMessageDto
    {
        // Ідентифікатор чату, в який надсилається повідомлення
        public int ChatRoomId { get; set; }
        public ChatRoomType ChatRoomType { get; set; }

        // Текст повідомлення
        public string Content { get; set; } = null!;
    }
}
