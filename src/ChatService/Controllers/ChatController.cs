using ChatService.DTOs;
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

        // POST: api/chat/create
        // Створює новий чат
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatRoomDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { Message = "Невірні дані для створення чату." });
            }

            // Формування відповіді з даними створеного чату
            var response = await _chatService.CreateChatRoomAsync(model);

            return Ok(response);
        }

        // GET: api/chat
        // Отримує список чатів без папки
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetChatRoomsForUser()
        {
            Console.WriteLine("IN GetChatRoomsForUser");
            var response = await _chatService.GetChatsWithoutFolder();

            return Ok(response);
        }

        // GET: api/chat/folder/{Id}
        // Отримує список чатів з папки
        [Authorize]
        [HttpGet("folder/{Id}")]
        public async Task<IActionResult> GetChatRoomsForFolder(int Id)
        {
            var response = await _chatService.GetChatsForFolder(Id);

            return Ok(response);
        }
    }
}
