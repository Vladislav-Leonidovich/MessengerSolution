using ChatServiceDTOs.Chats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Attributes;
using System.Security.Claims;
using Shared.Authorization.Permissions;
using ChatService.Services.Interfaces;

namespace ChatService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        // GET: api/chat/private/{chatRoomId}
        [HttpGet("private/{chatRoomId}")]
        [RequireChatAccess] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetPrivateChatById(int chatRoomId)
        {
            var userId = GetUserId();
            var result = await _chatService.GetPrivateChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // GET: api/chat/group/{chatRoomId}
        [HttpGet("group/{chatRoomId}")]
        [RequireChatAccess] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetGroupChatById(int chatRoomId)
        {
            var userId = GetUserId();
            var result = await _chatService.GetGroupChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // GET: api/chat/private
        [HttpGet("private")]
        public async Task<IActionResult> GetPrivateChatsForUser()
        {
            var userId = GetUserId();
            var result = await _chatService.GetPrivateChatsForUserAsync(userId);
            return Ok(result);
        }

        // GET: api/chat/group
        [HttpGet("group")]
        public async Task<IActionResult> GetGroupChatsForUser()
        {
            var userId = GetUserId();
            var result = await _chatService.GetGroupChatsForUserAsync(userId);
            return Ok(result);
        }

        // POST: api/chat/create-private
        [HttpPost("create-private")]
        public async Task<IActionResult> CreatePrivateChat([FromBody] CreatePrivateChatRoomDto model)
        {
            var userId = GetUserId();
            var result = await _chatService.CreatePrivateChatAsync(model, userId);
            return CreatedAtAction(nameof(GetPrivateChatById), new { chatRoomId = result.Data?.Id }, result);
        }

        // DELETE: api/chat/delete-private/{chatRoomId}
        [HttpDelete("delete-private/{chatRoomId}")]
        [RequireChatAccess] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeletePrivateChat(int chatRoomId)
        {
            var userId = GetUserId();
            var result = await _chatService.DeletePrivateChatAsync(chatRoomId, userId);
            return Ok(result);
        }

        // DELETE: api/chat/delete-group/{chatRoomId}
        [HttpDelete("delete-group/{chatRoomId}")]
        [RequireChatAccess] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeleteGroupChat(int chatRoomId)
        {
            var userId = GetUserId();
            var result = await _chatService.DeleteGroupChatAsync(chatRoomId, userId);
            return Ok(result);
        }

        // GET: api/chat/get-auth-user-in-chat/{chatRoomId}
        [HttpGet("get-auth-user-in-chat/{chatRoomId}")]
        public async Task<IActionResult> IsAuthUserInChatRoom(int chatRoomId)
        {
            var userId = GetUserId();
            var result = await _chatService.IsUserInChatAsync(userId, chatRoomId);
            return Ok(result);
        }

        // Допоміжний метод для отримання ID користувача з токена
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }
            return userId;
        }
    }
}
