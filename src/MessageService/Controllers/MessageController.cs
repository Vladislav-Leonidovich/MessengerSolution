using MessageServiceDTOs;
using MessageService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MessageService.Controllers
{
    [Authorize]
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

            var response = await _messageService.SendMessageAsync(model);
            // Повертаємо відповідь із даними створеного повідомлення
            return Ok(response);
        }

        // GET: api/message/chat/{chatRoomId}?pageNumber=1&pageSize=20
        // Отримує список повідомлень для зазначеного чату
        [HttpGet("chat/{chatRoomId}")]
        public async Task<IActionResult> GetMessages(int chatRoomId, [FromQuery] int startIndex = 1, [FromQuery] int count = 20)
        {
            var response = await _messageService.GetMessagesAsync(chatRoomId, startIndex, count);
            return Ok(response);
        }

        // PUT: api/message/mark-as-read/{messageId}
        // Позначає повідомлення як прочитане
        [HttpPut("mark-as-read/{messageId}")]
        public async Task<IActionResult> MarkMessageAsRead(int messageId)
        {
            var response = await _messageService.MarkMessageAsRead(messageId);
            return Ok(response);
        }

        // GET: api/message/get-last-message/{chatRoomId}
        // Отримує останнє повідомлення для попереднього перегляду
        [HttpGet("get-last-message/{chatRoomId}")]
        public async Task<IActionResult> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId)
        {
            var responce = await _messageService.GetLastMessagePreviewByChatRoomIdAsync(chatRoomId);
            if(responce == null)
            {
                return NotFound();
            }
            return Ok(responce);
        }

        // DELETE: api/message/delete/{messageId}
        // Видаляє повідомлення
        [HttpDelete("delete/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var response = await _messageService.DeleteMessageAsync(messageId);
            return Ok(response);
        }

        // DELETE: api/message/delete-by-chat/{chatRoomId}
        // Видаляє всі повідомлення для зазначеного чату
        [HttpDelete("delete-by-chat/{chatRoomId}")]
        public async Task<IActionResult> DeleteMessagesByChatRoomId(int chatRoomId)
        {
            var response = await _messageService.DeleteMessagesByChatRoomIdAsync(chatRoomId);
            return Ok(response);
        }

        // GET: api/message/count/{chatRoomId}
        // Отримує кількість повідомлень для зазначеного чату
        [HttpGet("count/{chatRoomId}")]
        public async Task<IActionResult> GetMessagesCountByChatRoomId(int chatRoomId)
        {
            var response = await _messageService.GetMessagesCountByChatRoomIdAsync(chatRoomId);
            return Ok(response);
        }
    }
}
