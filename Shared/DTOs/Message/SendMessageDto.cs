using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Message
{
    public class SendMessageDto
    {
        // Ідентифікатор чату, в який надсилається повідомлення
        [Required]
        public int ChatRoomId { get; set; }

        // Текст повідомлення
        [Required]
        [MinLength(1)]
        [MaxLength(4000)]
        public string Content { get; set; } = null!;
    }
}
