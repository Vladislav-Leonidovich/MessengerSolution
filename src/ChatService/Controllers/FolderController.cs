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
        // GET: api/folder
        // Отримує список папок для вказаного користувача
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFolders()
        {
            var folders = await _folderService.GetFoldersAsync();
            return Ok(folders);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderDto folderDto)
        {
            var createdFolder = await _folderService.CreateFolderAsync(folderDto);
            return CreatedAtAction(nameof(GetFolders), new { id = createdFolder.Id }, createdFolder);
        }

        // Ендпоінт для оновлення даних папки
        // Використовуємо PUT, щоб оновити всю інформацію про папку
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFolder(int id, [FromBody] FolderDto folderDto)
        {
            if (id != folderDto.Id)
            {
                return BadRequest("Ідентифікатори не співпадають.");
            }

            var result = await _folderService.UpdateFolderAsync(folderDto);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        // Ендпоінт для видалення папки
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var result = await _folderService.DeleteFolderAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        // Ендпоінт для призначення чату до папки
        // Наприклад, шлях: api/folder/{folderId}/assign-chat/{chatId}
        [Authorize]
        [HttpPost("{folderId}/assign-chat/{chatId}")]
        public async Task<IActionResult> AssignChatToFolder(int folderId, int chatId)
        {
            var result = await _folderService.AssignChatToFolderAsync(chatId, folderId);
            if (!result)
            {
                return NotFound("Чат або папку не знайдено.");
            }
            return Ok();
        }
    }
}
