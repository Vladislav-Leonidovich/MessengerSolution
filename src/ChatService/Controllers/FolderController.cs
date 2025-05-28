using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Attributes;
using System.Security.Claims;
using Shared.Authorization.Permissions;
using ChatService.Services.Interfaces;
using Shared.DTOs.Folder;

namespace ChatService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FolderController : ControllerBase
    {
        private readonly IFolderService _folderService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FolderController> _logger;

        public FolderController(IFolderService folderService, IHttpContextAccessor httpContextAccessor, ILogger<FolderController> logger)
        {
            _folderService = folderService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // GET: api/folder
        [HttpGet]
        public async Task<IActionResult> GetFolders()
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.GetFoldersAsync(userId);
            return Ok(result);
        }

        // GET: api/folder/{folderId}
        [HttpGet("{folderId}")]
        [RequirePermission(ChatPermission.ViewFolder)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> GetFolderById(int folderId)
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.GetFolderByIdAsync(folderId, userId);
            return Ok(result);
        }

        // POST: api/folder/create
        [HttpPost("create")]
        [RequirePermission(ChatPermission.CreateFolder)]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto model)
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.CreateFolderAsync(model, userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetFolderById), new { folderId = result.Data?.Id }, result);
        }

        // PUT: api/folder/update
        [HttpPut("update")]
        [RequirePermission(ChatPermission.UpdateFolder)] // Використовуємо атрибут для перевірки доступу через FolderId в моделі
        public async Task<IActionResult> UpdateFolder([FromBody] FolderDto model)
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.UpdateFolderAsync(model, userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return NoContent();
        }

        // DELETE: api/folder/delete/{folderId}
        [HttpDelete("delete/{folderId}")]
        [RequirePermission(ChatPermission.DeleteFolder)] // Використовуємо атрибут для перевірки доступу
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.DeleteFolderAsync(folderId, userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return NoContent();
        }

        // POST: api/folder/{folderId}/assign-private-chat/{chatId}
        [HttpPost("{folderId}/assign-private-chat/{chatId}")]
        [RequirePermission(ChatPermission.AssignChatToFolder)] // Перевіряємо доступ до папки
        public async Task<IActionResult> AssignPrivateChatToFolder(int folderId, int chatId)
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.AssignChatToFolderAsync(chatId, folderId, false, userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        // POST: api/folder/{folderId}/unassign-private-chat/{chatId}
        [HttpPost("unassign-private-chat/{chatId}")]
        [RequirePermission(ChatPermission.UnassignChatToFolder)] // Перевіряємо доступ до папки
        public async Task<IActionResult> UnassignPrivateChatFromFolder(int chatId)
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.UnassignChatFromFolderAsync(chatId, false, userId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        // Допоміжний метод для отримання ID користувача з токена
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
