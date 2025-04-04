﻿using ChatServiceDTOs.Chats;
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
        // Створює новий приватний чат
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
        // Створює новий груповий чат
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

        // GET: api/chat/get-auth-user-in-chat/{chatRoomId}
        // Перевіряє, чи є залогінений користувач у чаті
        [HttpGet("get-auth-user-in-chat/{chatRoomId}")]
        public async Task<IActionResult> IsAuthUserInChatRoomsByChatRoomId(int chatRoomId)
        {
            var response = await _chatService.IsAuthUserInChatRoomsByChatRoomIdAsync(chatRoomId);
            return Ok(response);
        }

        // DELETE: api/chat/delete-private/{privateChatId}
        // Видаляє приватний чат
        [HttpDelete("delete-private/{privateChatId}")]
        public async Task<IActionResult> DeletePrivateChat(int privateChatId)
        {
            var response = await _chatService.DeletePrivateСhatAsync(privateChatId);
            if(response)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(new { Message = "Не вдалося видалити приватний чат." });
            }
        }

        // DELETE: api/chat/delete-group/{groupChatId}
        // Видаляє груповий чат
        [HttpDelete("delete-group/{groupChatId}")]
        public async Task<IActionResult> DeleteGroupChat(int groupChatId)
        {
            var response = await _chatService.DeleteGroupСhatAsync(groupChatId);
            if (response)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(new { Message = "Не вдалося видалити груповий чат." });
            }
        }

        // GET: api/chat/private/{chatRoomId}
        // Отримує приватний чат за його ідентифікатором
        [HttpGet("private/{chatRoomId}")]
        public async Task<IActionResult> GetPrivateChatById(int chatRoomId)
        {
            try
            {
                var chat = await _chatService.GetPrivateChatByIdAsync(chatRoomId);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // GET: api/chat/group/{chatRoomId}
        // Отримує груповий чат за його ідентифікатором
        [HttpGet("group/{chatRoomId}")]
        public async Task<IActionResult> GetGroupChatById(int chatRoomId)
        {
            try
            {
                var chat = await _chatService.GetGroupChatByIdAsync(chatRoomId);
                return Ok(chat);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
