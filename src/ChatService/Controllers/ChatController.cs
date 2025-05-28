using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Attributes;
using System.Security.Claims;
using Shared.Authorization.Permissions;
using ChatService.Services.Interfaces;
using Shared.DTOs.Chat;

namespace ChatService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, IHttpContextAccessor httpContextAccessor, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        [HttpGet("{chatRoomId}")]
        [RequirePermission(ChatPermission.ViewChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetChatById(int chatRoomId)
        {
            var userId = GetCurrentUserId();

            var result = await _chatService.GetChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // GET: api/chat/private/{chatRoomId}
        [HttpGet("private/{chatRoomId}")]
        [RequirePermission(ChatPermission.ViewChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetPrivateChatById(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetPrivateChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // GET: api/chat/group/{chatRoomId}
        [HttpGet("group/{chatRoomId}")]
        [RequirePermission(ChatPermission.ViewChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetGroupChatById(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetGroupChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // GET: api/chat/private
        [HttpGet("private")]
        public async Task<IActionResult> GetPrivateChatsForUser()
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetPrivateChatsForUserAsync(userId);
            return Ok(result);
        }

        // GET: api/chat/group
        [HttpGet("group")]
        public async Task<IActionResult> GetGroupChatsForUser()
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetGroupChatsForUserAsync(userId);
            return Ok(result);
        }

        // POST: api/chat/create-private
        [HttpPost("create-private")]
        [RequirePermission(ChatPermission.CreateChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> CreatePrivateChat([FromBody] CreatePrivateChatRoomDto model)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.CreatePrivateChatAsync(model, userId);
            return CreatedAtAction(nameof(GetPrivateChatById), new { chatRoomId = result.Data?.Id }, result);
        }

        [HttpDelete("delete/{chatRoomId}")]
        [RequirePermission(ChatPermission.DeleteChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeleteChatById(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.DeleteChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // DELETE: api/chat/delete-private/{chatRoomId}
        [HttpDelete("delete-private/{chatRoomId}")]
        [RequirePermission(ChatPermission.DeleteChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeletePrivateChatById(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.DeletePrivateChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // DELETE: api/chat/delete-group/{chatRoomId}
        [HttpDelete("delete-group/{chatRoomId}")]
        [RequirePermission(ChatPermission.DeleteChat)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeleteGroupChatById(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.DeleteGroupChatByIdAsync(chatRoomId, userId);
            return Ok(result);
        }

        // GET: api/chat/get-auth-user-in-chat/{chatRoomId}
        [HttpGet("get-auth-user-in-chat/{chatRoomId}")]
        public async Task<IActionResult> IsAuthUserInChatRoom(int chatRoomId)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.IsUserInChatAsync(userId, chatRoomId);
            return Ok(result);
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
