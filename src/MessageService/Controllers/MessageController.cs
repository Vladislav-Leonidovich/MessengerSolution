using MessageService.DTOs;
using MessageService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        // POST: api/message/send
        // Створює нове повідомлення
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Content))
            {
                return BadRequest(new { Message = "Неправильні дані для надсилання повідомлення." });
            }

            var message = await _messageService.SendMessageAsync(model);
            // Повертаємо відповідь із даними створеного повідомлення
            return Ok(new MessageDto
            {
                Id = message.Id,
                ChatRoomId = message.ChatRoomId,
                SenderUserId = message.SenderUserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            });
        }

        // GET: api/message/chat/{chatRoomId}?pageNumber=1&pageSize=20
        // Отримує список повідомлень для зазначеного чату
        [HttpGet("chat/{chatRoomId}")]
        public async Task<IActionResult> GetMessages(int chatRoomId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var messages = await _messageService.GetMessagesAsync(chatRoomId, pageNumber, pageSize);
            // Перетворюємо кожне повідомлення в MessageResponse для уніфікованої відповіді
            var response = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ChatRoomId = m.ChatRoomId,
                SenderUserId = m.SenderUserId,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            });
            return Ok(response);
        }
    }
}
