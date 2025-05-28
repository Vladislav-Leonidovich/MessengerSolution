using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MessageService.Services.Interfaces;
using System.Security.Claims;
using MessageService.Attributes;
using Shared.Authorization.Permissions;
using Shared.DTOs.Message;

namespace MessageService.Controllers
{
    [Authorize]
    [ApiController]
    [ValidateModelState]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MessageController(IMessageService messageService, IHttpContextAccessor httpContextAccessor)
        {
            _messageService = messageService;
            _httpContextAccessor = httpContextAccessor;
        }

        // POST: api/message/send
        // Створює нове повідомлення
        [HttpPost("send")]
        [RequirePermission(MessagePermission.SendMessage)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto model)
        {
            var userId = GetCurrentUserId();
            var response = await _messageService.SendMessageViaSagaAsync(model, userId);
            // Повертаємо відповідь із даними створеного повідомлення
            return Ok(response);
        }

        // GET: api/message/chat/{chatRoomId}?pageNumber=1&pageSize=20
        // Отримує список повідомлень для зазначеного чату
        [HttpGet("chat/{chatRoomId}")]
        public async Task<IActionResult> GetMessages(int chatRoomId, [FromQuery] int startIndex = 1, [FromQuery] int count = 20)
        {
            var userId = GetCurrentUserId();
            var response = await _messageService.GetMessagesAsync(chatRoomId, userId, startIndex, count);
            return Ok(response);
        }

        [HttpPost("confirm-delivery")]
        public async Task<IActionResult> ConfirmMessageDelivery([FromBody] ConfirmDeliveryDto model)
        {
            var userId = GetCurrentUserId();
            var response = await _messageService.ConfirmMessageDeliveryAsync(model.MessageId, userId);
            return Ok(response);
        }

        // PUT: api/message/mark-as-read/{messageId}
        // Позначає повідомлення як прочитане
        [HttpPut("mark-as-read/{messageId}")]
        [RequirePermission(MessagePermission.ViewMessage)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> MarkMessageAsRead(int messageId)
        {
            var userId = GetCurrentUserId();
            var response = await _messageService.MarkMessageAsRead(messageId, userId);
            return Ok(response);
        }

        // GET: api/message/get-last-message/{chatRoomId}
        // Отримує останнє повідомлення для попереднього перегляду
        [HttpGet("get-last-message/{chatRoomId}")]
        [RequirePermission(MessagePermission.ViewMessage)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var responce = await _messageService.GetLastMessagePreviewByChatRoomIdAsync(chatRoomId, userId);
            if(responce == null)
            {
                return NotFound();
            }
            return Ok(responce);
        }

        // DELETE: api/message/delete/{messageId}
        // Видаляє повідомлення
        [HttpDelete("delete/{messageId}")]
        [RequirePermission(MessagePermission.DeleteMessage)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = GetCurrentUserId();
            var response = await _messageService.DeleteMessageAsync(messageId, userId);
            return Ok(response);
        }

        // DELETE: api/message/delete-by-chat/{chatRoomId}
        // Видаляє всі повідомлення для зазначеного чату
        [HttpDelete("delete-by-chat/{chatRoomId}")]
        [RequirePermission(MessagePermission.CleanChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeleteMessagesByChatRoomId(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var response = await _messageService.DeleteMessagesByChatRoomIdAsync(chatRoomId, userId);
            return Ok(response);
        }

        // GET: api/message/count/{chatRoomId}
        // Отримує кількість повідомлень для зазначеного чату
        [HttpGet("count/{chatRoomId}")]
        [RequirePermission(MessagePermission.ViewMessage)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetMessagesCountByChatRoomId(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var response = await _messageService.GetMessagesCountByChatRoomIdAsync(chatRoomId, userId);
            return Ok(response);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }
            return userId;
        }
    }
}
