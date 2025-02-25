using ChatServiceDTOs.Chats;
using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // POST: api/chat/create-private
        // Створює новий чат
        [HttpPost("create-private")]
        public async Task<IActionResult> CreatePrivateChat([FromBody] CreatePrivateChatRoomDto model)
        {
            if (model == null)
            {
                return BadRequest(new { Message = "Невірні дані для створення приватного чату." });
            }

            var response = await _chatService.CreatePrivateChatRoomAsync(model);
            return Ok(response);
        }

        // POST: api/chat/create-group
        // Створює новий чат
        [HttpPost("create-group")]
        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatRoomDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { Message = "Невірні дані для створення групового чату." });
            }

            var response = await _chatService.CreateGroupChatRoomAsync(model);
            return Ok(response);
        }

        // GET: api/chat/private
        // Отримує список чатів без папки
        [HttpGet("private")]
        public async Task<IActionResult> GetPrivateChatsForUser()
        {
            var chats = await _chatService.GetPrivateChatRoomsForUserAsync();
            return Ok(chats);
        }

        // GET: api/chat/group
        // Отримує список чатів без папки
        [HttpGet("group")]
        public async Task<IActionResult> GetGroupChatsForUser()
        {
            var chats = await _chatService.GetGroupChatRoomsForUserAsync();
            return Ok(chats);
        }

        // GET: api/chat/private/folder/{folderId}
        // Отримує список приватних чатів з папки
        [HttpGet("private/folder/{folderId}")]
        public async Task<IActionResult> GetPrivateChatsForFolder(int folderId)
        {
            var chats = await _chatService.GetPrivateChatsForFolderAsync(folderId);
            return Ok(chats);
        }

        // GET: api/chat/group/folder/{folderId}
        // Отримує список групових чатів з папки
        [HttpGet("group/folder/{folderId}")]
        public async Task<IActionResult> GetGroupChatsForFolder(int folderId)
        {
            var chats = await _chatService.GetGroupChatsForFolderAsync(folderId);
            return Ok(chats);
        }

        // GET: api/chat/private/no-folder
        // Отримує список приватних чатів без папки
        [HttpGet("private/no-folder")]
        public async Task<IActionResult> GetPrivateChatsWithoutFolder()
        {
            var chats = await _chatService.GetPrivateChatsWithoutFolderAsync();
            return Ok(chats);
        }

        // GET: api/chat/group/no-folder
        // Отримує список групових чатів без папки
        [HttpGet("group/no-folder")]
        public async Task<IActionResult> GetGroupChatsWithoutFolder()
        {
            var chats = await _chatService.GetGroupChatsWithoutFolderAsync();
            return Ok(chats);
        }
    }
}
