using ChatServiceDTOs.Folders;
using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FolderController : Controller
    {
        private readonly IFolderService _folderService;

        public FolderController(IFolderService folderService)
        {
            _folderService = folderService;
        }

        // Отримує список папок
        // GET: api/folder
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFolders()
        {
            var folders = await _folderService.GetFoldersAsync();
            return Ok(folders);
        }

        // Створює папку
        // POST: api/folder/create
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto folderDto)
        {
            var createdFolder = await _folderService.CreateFolderAsync(folderDto);
            return CreatedAtAction(nameof(GetFolders), new { id = createdFolder.Id }, createdFolder);
        }

        // Ендпоінт для оновлення даних папки
        // PUT: api/folder/update
        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateFolder([FromBody] FolderDto folderDto)
        {
            var result = await _folderService.UpdateFolderAsync(folderDto);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        // Ендпоінт для видалення папки
        // DELETE: api/folder/delete/{folderId}
        [Authorize]
        [HttpDelete("delete/{folderId}")]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            var result = await _folderService.DeleteFolderAsync(folderId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        // Ендпоінт для призначення приватного чату до папки
        // POST: api/folder/{folderId}/assign-private-chat/{chatId}
        [Authorize]
        [HttpPost("{folderId}/assign-private-chat/{chatId}")]
        public async Task<IActionResult> AssignPrivateChatToFolder(int folderId, int chatId)
        {
            var result = await _folderService.AssignPrivateChatToFolderAsync(chatId, folderId);
            if (!result)
            {
                return NotFound("Чат або папку не знайдено.");
            }
            return Ok();
        }

        // Ендпоінт для від'єднання приватного чату від папки
        // POST: api/folder/{folderId}/unassign-private-chat/{chatId}
        [Authorize]
        [HttpPost("{folderId}/unassign-private-chat/{chatId}")]
        public async Task<IActionResult> UnassignPrivateChatFromFolder(int chatId)
        {
            var result = await _folderService.UnassignPrivateChatFromFolderAsync(chatId);
            if (!result)
            {
                return NotFound("Чат або папку не знайдено.");
            }
            return Ok();
        }

        // Ендпоінт для призначення групового чату до папки
        // POST: api/folder/{folderId}/assign-group-chat/{chatId}
        [Authorize]
        [HttpPost("{folderId}/assign-group-chat/{chatId}")]
        public async Task<IActionResult> AssignGroupChatToFolder(int folderId, int chatId)
        {
            var result = await _folderService.AssignGroupChatToFolderAsync(chatId, folderId);
            if (!result)
            {
                return NotFound("Чат або папку не знайдено.");
            }
            return Ok();
        }

        // Ендпоінт для від'єднання групового чату від папки
        // POST: api/folder/{folderId}/unassign-group-chat/{chatId}
        [Authorize]
        [HttpPost("{folderId}/unassign-group-chat/{chatId}")]
        public async Task<IActionResult> UnassignGroupChatFromFolder(int chatId)
        {
            var result = await _folderService.UnassignGroupChatFromFolderAsync(chatId);
            if (!result)
            {
                return NotFound("Чат або папку не знайдено.");
            }
            return Ok();
        }
    }
}
